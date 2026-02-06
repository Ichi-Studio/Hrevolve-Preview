using Hrevolve.Agent.Configuration;
using Hrevolve.Agent.Models.Text2Sql;
using Microsoft.Extensions.Options;

namespace Hrevolve.Agent.Services.Text2Sql;

/// <summary>
/// SQL 安全验证器实现
/// </summary>
public class SqlSecurityValidator : ISqlSecurityValidator
{
    private readonly Text2SqlOptions _options;
    private readonly ISchemaProvider _schemaProvider;

    public SqlSecurityValidator(IOptions<Text2SqlOptions> options, ISchemaProvider schemaProvider)
    {
        _options = options.Value;
        _schemaProvider = schemaProvider;
    }

    public ValidationResult Validate(QueryRequest request)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        // 1. 验证实体白名单
        var entityValidation = ValidateEntity(request.TargetEntity);
        if (!entityValidation.IsValid)
        {
            return entityValidation;
        }

        // 2. 验证操作类型
        var operationValidation = ValidateOperation(request.Operation);
        if (!operationValidation.IsValid)
        {
            return operationValidation;
        }

        // 3. 验证字段白名单
        var fieldValidation = ValidateFields(request);
        if (!fieldValidation.IsValid)
        {
            errors.AddRange(fieldValidation.Errors);
        }

        // 4. 验证 JOIN 数量
        if (request.Joins.Count > _options.MaxJoinTables)
        {
            errors.Add(new ValidationError
            {
                Code = ValidationErrorCodes.TooManyJoins,
                Message = $"JOIN 表数量超过限制，最多允许 {_options.MaxJoinTables} 张表，当前为 {request.Joins.Count} 张"
            });
        }

        // 5. 验证 JOIN 的实体
        foreach (var join in request.Joins)
        {
            if (!_options.AllowedEntities.Contains(join.Entity, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(new ValidationError
                {
                    Code = ValidationErrorCodes.EntityNotAllowed,
                    Message = $"不允许 JOIN 实体 '{join.Entity}'",
                    Field = join.Entity
                });
            }
        }

        // 6. 验证过滤条件数量
        if (request.Filters.Count > _options.MaxFilters)
        {
            errors.Add(new ValidationError
            {
                Code = ValidationErrorCodes.TooManyFilters,
                Message = $"过滤条件数量超过限制，最多允许 {_options.MaxFilters} 个，当前为 {request.Filters.Count} 个"
            });
        }

        // 7. 验证过滤条件中的字段
        foreach (var filter in request.Filters)
        {
            var filterFieldValidation = ValidateFilterField(filter, request.TargetEntity, request.Joins);
            if (!filterFieldValidation.IsValid)
            {
                errors.AddRange(filterFieldValidation.Errors);
            }
        }

        // 8. 验证结果集大小
        if (request.Limit > _options.MaxResultRows)
        {
            warnings.Add(new ValidationWarning
            {
                Code = "LIMIT_ADJUSTED",
                Message = $"返回行数已从 {request.Limit} 调整为最大值 {_options.MaxResultRows}"
            });
            request.Limit = _options.MaxResultRows;
        }

        // 9. 验证复杂度
        var complexity = CalculateComplexityScore(request);
        if (complexity > _options.MaxComplexityScore)
        {
            errors.Add(new ValidationError
            {
                Code = ValidationErrorCodes.QueryTooComplex,
                Message = $"查询复杂度 ({complexity}) 超过限制 ({_options.MaxComplexityScore})，请简化查询"
            });
        }

        // 10. 验证更新/插入操作的值
        if (request.Operation is QueryOperation.Insert or QueryOperation.Update)
        {
            var updateValidation = ValidateUpdateValues(request);
            if (!updateValidation.IsValid)
            {
                errors.AddRange(updateValidation.Errors);
            }
        }

        if (errors.Count > 0)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            };
        }

        return new ValidationResult
        {
            IsValid = true,
            Warnings = warnings,
            CorrectedRequest = request
        };
    }

    public ValidationResult ValidateRawText(string queryText)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            return ValidationResult.Ok();
        }

        var lowerText = queryText.ToLowerInvariant();
        var foundKeywords = _options.BlockedKeywords
            .Where(keyword => lowerText.Contains(keyword.ToLowerInvariant()))
            .ToList();

        if (foundKeywords.Count > 0)
        {
            return ValidationResult.Fail(new ValidationError
            {
                Code = ValidationErrorCodes.DangerousKeyword,
                Message = $"查询包含不安全的关键字: {string.Join(", ", foundKeywords)}",
                Details = "请修改查询以移除这些关键字"
            });
        }

        return ValidationResult.Ok();
    }

    public int CalculateComplexityScore(QueryRequest request)
    {
        var score = 0;

        // JOIN 数量：每个 +5 分
        score += request.Joins.Count * 5;

        // 过滤条件：每个 +2 分
        score += request.Filters.Count * 2;

        // 聚合查询：+10 分
        if (request.Aggregation.HasValue)
        {
            score += 10;
        }

        // 分组查询：每个字段 +5 分
        score += request.GroupByFields.Count * 5;

        // 排序：每个 +1 分
        score += request.OrderBy.Count;

        // 修改操作：+15 分
        if (request.Operation != QueryOperation.Select)
        {
            score += 15;
        }

        // 大结果集：超过 500 行 +10 分
        if (request.Limit > 500)
        {
            score += 10;
        }

        return score;
    }

    private ValidationResult ValidateEntity(string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return ValidationResult.Fail(ValidationErrorCodes.EntityNotAllowed, "未指定目标实体");
        }

        if (!_options.AllowedEntities.Contains(entityName, StringComparer.OrdinalIgnoreCase))
        {
            return ValidationResult.Fail(new ValidationError
            {
                Code = ValidationErrorCodes.EntityNotAllowed,
                Message = $"不允许访问实体 '{entityName}'",
                Field = entityName,
                Details = $"允许的实体: {string.Join(", ", _options.AllowedEntities)}"
            });
        }

        if (!_schemaProvider.EntityExists(entityName))
        {
            return ValidationResult.Fail(new ValidationError
            {
                Code = ValidationErrorCodes.EntityNotAllowed,
                Message = $"实体 '{entityName}' 不存在",
                Field = entityName
            });
        }

        return ValidationResult.Ok();
    }

    private ValidationResult ValidateOperation(QueryOperation operation)
    {
        if (operation == QueryOperation.Select)
        {
            return ValidationResult.Ok();
        }

        if (!_options.EnableCrud)
        {
            return ValidationResult.Fail(new ValidationError
            {
                Code = ValidationErrorCodes.OperationNotAllowed,
                Message = "CRUD 操作已禁用，仅允许查询操作"
            });
        }

        return ValidationResult.Ok();
    }

    private ValidationResult ValidateFields(QueryRequest request)
    {
        var errors = new List<ValidationError>();
        var entitySchema = _schemaProvider.GetEntitySchema(request.TargetEntity);
        
        if (entitySchema == null)
        {
            return ValidationResult.Fail(ValidationErrorCodes.EntityNotAllowed, $"实体 '{request.TargetEntity}' 不存在");
        }

        foreach (var fieldName in request.SelectFields)
        {
            // 支持导航属性（如 Employee.FirstName）
            var parts = fieldName.Split('.');
            var actualEntity = parts.Length > 1 ? parts[0] : request.TargetEntity;
            var actualField = parts.Length > 1 ? parts[1] : fieldName;

            var fieldSchema = _schemaProvider.GetFieldSchema(actualEntity, actualField);
            if (fieldSchema == null)
            {
                errors.Add(new ValidationError
                {
                    Code = ValidationErrorCodes.FieldNotAllowed,
                    Message = $"字段 '{fieldName}' 不存在于实体 '{actualEntity}'",
                    Field = fieldName
                });
            }
        }

        return errors.Count > 0 
            ? new ValidationResult { IsValid = false, Errors = errors } 
            : ValidationResult.Ok();
    }

    private ValidationResult ValidateFilterField(FilterCondition filter, string targetEntity, List<JoinClause> joins)
    {
        var parts = filter.Field.Split('.');
        var entityName = parts.Length > 1 ? parts[0] : targetEntity;
        var fieldName = parts.Length > 1 ? parts[1] : filter.Field;

        // 验证实体是否在查询范围内
        var validEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { targetEntity };
        foreach (var join in joins)
        {
            validEntities.Add(join.Entity);
            if (!string.IsNullOrEmpty(join.Alias))
            {
                validEntities.Add(join.Alias);
            }
        }

        if (!validEntities.Contains(entityName))
        {
            return ValidationResult.Fail(new ValidationError
            {
                Code = ValidationErrorCodes.EntityNotAllowed,
                Message = $"过滤条件引用的实体 '{entityName}' 不在查询范围内",
                Field = filter.Field
            });
        }

        // 验证字段是否存在
        var fieldSchema = _schemaProvider.GetFieldSchema(entityName, fieldName);
        if (fieldSchema == null)
        {
            return ValidationResult.Fail(new ValidationError
            {
                Code = ValidationErrorCodes.FieldNotAllowed,
                Message = $"字段 '{fieldName}' 不存在于实体 '{entityName}'",
                Field = filter.Field
            });
        }

        // 验证字段是否可用于过滤
        if (!fieldSchema.IsFilterable)
        {
            return ValidationResult.Fail(new ValidationError
            {
                Code = ValidationErrorCodes.FieldNotAllowed,
                Message = $"字段 '{fieldName}' 不可用于过滤",
                Field = filter.Field
            });
        }

        return ValidationResult.Ok();
    }

    private ValidationResult ValidateUpdateValues(QueryRequest request)
    {
        var errors = new List<ValidationError>();
        var entitySchema = _schemaProvider.GetEntitySchema(request.TargetEntity);

        if (entitySchema == null)
        {
            return ValidationResult.Fail(ValidationErrorCodes.EntityNotAllowed, $"实体 '{request.TargetEntity}' 不存在");
        }

        // 检查实体是否支持 CRUD
        if (!entitySchema.SupportsCrud)
        {
            return ValidationResult.Fail(new ValidationError
            {
                Code = ValidationErrorCodes.OperationNotAllowed,
                Message = $"实体 '{request.TargetEntity}' 不支持修改操作"
            });
        }

        foreach (var (fieldName, _) in request.UpdateValues)
        {
            var fieldSchema = entitySchema.Fields.FirstOrDefault(f => 
                f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            if (fieldSchema == null)
            {
                errors.Add(new ValidationError
                {
                    Code = ValidationErrorCodes.FieldNotAllowed,
                    Message = $"字段 '{fieldName}' 不存在",
                    Field = fieldName
                });
                continue;
            }

            // 检查只读字段
            if (fieldSchema.IsReadOnly)
            {
                errors.Add(new ValidationError
                {
                    Code = ValidationErrorCodes.FieldNotAllowed,
                    Message = $"字段 '{fieldName}' 为只读，不可修改",
                    Field = fieldName
                });
            }

            // 检查主键（不允许通过 Text2SQL 修改主键）
            if (fieldSchema.IsPrimaryKey && request.Operation == QueryOperation.Update)
            {
                errors.Add(new ValidationError
                {
                    Code = ValidationErrorCodes.FieldNotAllowed,
                    Message = $"不允许修改主键字段 '{fieldName}'",
                    Field = fieldName
                });
            }
        }

        return errors.Count > 0
            ? new ValidationResult { IsValid = false, Errors = errors }
            : ValidationResult.Ok();
    }
}
