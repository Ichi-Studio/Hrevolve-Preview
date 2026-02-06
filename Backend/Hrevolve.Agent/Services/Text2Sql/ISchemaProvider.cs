using Hrevolve.Agent.Models.Text2Sql;

namespace Hrevolve.Agent.Services.Text2Sql;

/// <summary>
/// Schema 提供者接口 - 提供数据库实体的结构描述
/// </summary>
public interface ISchemaProvider
{
    /// <summary>
    /// 获取完整的 Schema 描述（包含所有可用实体）
    /// </summary>
    SchemaDescriptor GetSchema();
    
    /// <summary>
    /// 获取指定实体的 Schema 描述
    /// </summary>
    /// <param name="entityName">实体名称</param>
    /// <returns>实体 Schema，如果不存在则返回 null</returns>
    EntitySchema? GetEntitySchema(string entityName);
    
    /// <summary>
    /// 获取所有可用实体名称
    /// </summary>
    IReadOnlyList<string> GetAllEntityNames();
    
    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    bool EntityExists(string entityName);
    
    /// <summary>
    /// 检查字段是否存在于指定实体中
    /// </summary>
    bool FieldExists(string entityName, string fieldName);
    
    /// <summary>
    /// 获取字段 Schema
    /// </summary>
    FieldSchema? GetFieldSchema(string entityName, string fieldName);
    
    /// <summary>
    /// 根据中文别名查找实体名称
    /// </summary>
    string? FindEntityByAlias(string alias);
    
    /// <summary>
    /// 根据中文别名查找字段名称
    /// </summary>
    string? FindFieldByAlias(string entityName, string alias);
    
    /// <summary>
    /// 获取用于 LLM 提示词的 Schema 描述文本
    /// </summary>
    string GetPromptSchemaDescription();
    
    /// <summary>
    /// 获取查询示例（用于 Few-shot Learning）
    /// </summary>
    IReadOnlyList<QueryExample> GetQueryExamples();
}
