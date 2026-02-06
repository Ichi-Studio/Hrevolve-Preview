namespace Hrevolve.Agent.Models.Text2Sql;

/// <summary>
/// 实体 Schema 描述
/// </summary>
public class EntitySchema
{
    /// <summary>实体名称（英文）</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>实体中文名称</summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>实体描述</summary>
    public string? Description { get; set; }
    
    /// <summary>常用别名（用于自然语言识别）</summary>
    public List<string> Aliases { get; set; } = [];
    
    /// <summary>字段列表</summary>
    public List<FieldSchema> Fields { get; set; } = [];
    
    /// <summary>关系列表</summary>
    public List<RelationshipSchema> Relationships { get; set; } = [];
    
    /// <summary>是否支持 CRUD 操作</summary>
    public bool SupportsCrud { get; set; } = true;
    
    /// <summary>示例查询</summary>
    public List<QueryExample> Examples { get; set; } = [];
}

/// <summary>
/// 字段 Schema 描述
/// </summary>
public class FieldSchema
{
    /// <summary>字段名称（英文）</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>字段中文名称</summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>字段描述</summary>
    public string? Description { get; set; }
    
    /// <summary>数据类型</summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>是否可为空</summary>
    public bool IsNullable { get; set; }
    
    /// <summary>是否为主键</summary>
    public bool IsPrimaryKey { get; set; }
    
    /// <summary>是否为外键</summary>
    public bool IsForeignKey { get; set; }
    
    /// <summary>外键关联的实体</summary>
    public string? ForeignKeyEntity { get; set; }
    
    /// <summary>是否为敏感字段（需要权限验证）</summary>
    public bool IsSensitive { get; set; }
    
    /// <summary>访问该字段所需的最低权限</summary>
    public string? RequiredPermission { get; set; }
    
    /// <summary>常用别名（用于自然语言识别）</summary>
    public List<string> Aliases { get; set; } = [];
    
    /// <summary>枚举值（如果是枚举类型）</summary>
    public List<EnumValue>? EnumValues { get; set; }
    
    /// <summary>是否可用于过滤</summary>
    public bool IsFilterable { get; set; } = true;
    
    /// <summary>是否可用于排序</summary>
    public bool IsSortable { get; set; } = true;
    
    /// <summary>是否只读（不可用于 Insert/Update）</summary>
    public bool IsReadOnly { get; set; }
}

/// <summary>
/// 枚举值描述
/// </summary>
public class EnumValue
{
    /// <summary>枚举值名称</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>枚举值</summary>
    public int Value { get; set; }
    
    /// <summary>中文显示名称</summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// 关系描述
/// </summary>
public class RelationshipSchema
{
    /// <summary>关系名称</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>关联实体</summary>
    public string RelatedEntity { get; set; } = string.Empty;
    
    /// <summary>关系类型（OneToOne, OneToMany, ManyToOne, ManyToMany）</summary>
    public string RelationType { get; set; } = string.Empty;
    
    /// <summary>外键字段</summary>
    public string ForeignKey { get; set; } = string.Empty;
    
    /// <summary>关系描述</summary>
    public string? Description { get; set; }
}

/// <summary>
/// 查询示例（用于 Few-shot Learning）
/// </summary>
public class QueryExample
{
    /// <summary>自然语言输入</summary>
    public string Input { get; set; } = string.Empty;
    
    /// <summary>期望的查询请求</summary>
    public QueryRequest ExpectedOutput { get; set; } = new();
    
    /// <summary>说明</summary>
    public string? Description { get; set; }
}

/// <summary>
/// 完整的 Schema 描述（包含所有实体）
/// </summary>
public class SchemaDescriptor
{
    /// <summary>所有可用实体</summary>
    public List<EntitySchema> Entities { get; set; } = [];
    
    /// <summary>获取 LLM 提示词格式的 Schema 描述</summary>
    public string ToPromptFormat()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## 可用实体和字段");
        sb.AppendLine();
        
        foreach (var entity in Entities)
        {
            sb.AppendLine($"### {entity.Name} ({entity.DisplayName})");
            if (!string.IsNullOrEmpty(entity.Description))
            {
                sb.AppendLine($"描述: {entity.Description}");
            }
            if (entity.Aliases.Count > 0)
            {
                sb.AppendLine($"别名: {string.Join(", ", entity.Aliases)}");
            }
            sb.AppendLine();
            sb.AppendLine("字段:");
            
            foreach (var field in entity.Fields.Where(f => !f.IsSensitive))
            {
                var nullableStr = field.IsNullable ? "可空" : "必填";
                var aliasStr = field.Aliases.Count > 0 ? $" (别名: {string.Join(", ", field.Aliases)})" : "";
                sb.AppendLine($"- {field.Name} ({field.DisplayName}): {field.DataType}, {nullableStr}{aliasStr}");
                
                if (field.EnumValues?.Count > 0)
                {
                    var enumStr = string.Join(", ", field.EnumValues.Select(e => $"{e.Name}={e.DisplayName}"));
                    sb.AppendLine($"  可选值: {enumStr}");
                }
            }
            
            if (entity.Relationships.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("关系:");
                foreach (var rel in entity.Relationships)
                {
                    sb.AppendLine($"- {rel.Name} -> {rel.RelatedEntity} ({rel.RelationType})");
                }
            }
            
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
}
