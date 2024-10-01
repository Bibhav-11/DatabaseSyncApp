using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace CustomSyncLibrary;

public class FullTableSyncService
{
    public void SyncDatabases(DbContext sourceContext, DbContext targetContext)
    {
        var sourceDbSets = GetDbSetProperties(sourceContext);
        var targetDbSets = GetDbSetProperties(targetContext);

        foreach (var sourceDbSetProperty in sourceDbSets)
        {
            var entityType = sourceDbSetProperty.PropertyType.GenericTypeArguments.First();

            var targetDbSetProperty =
                targetDbSets.FirstOrDefault(t => t.PropertyType.GenericTypeArguments.FirstOrDefault() == entityType);

            if (targetDbSetProperty == null) continue;

            var sourceDbSet = sourceDbSetProperty.GetValue(sourceContext);
            var targetDbSet = targetDbSetProperty.GetValue(targetContext);

            SyncTableData(sourceDbSet, targetDbSet, sourceContext, targetContext);
        } 
        targetContext.SaveChanges();
    }
    
    private List<PropertyInfo> GetDbSetProperties(DbContext context)
    {
        return context.GetType()
            .GetProperties()
            .Where(p =>
                p.PropertyType.IsGenericType && 
                p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
            ).ToList();
    } 

    private void SyncTableData(object sourceDbSet, object targetDbSet, DbContext sourceContext, DbContext targetContext)
    {
        // var sourceList = GetEntityList(sourceDbSet);
        // var targetList = GetEntityList(targetDbSet);
        var sourceEntities = ((IQueryable<object>)sourceDbSet).AsNoTracking().ToList();
        var targetEntities = ((IQueryable<object>)targetDbSet).ToList();

        var entityType = sourceDbSet.GetType().GenericTypeArguments.First();

        var primaryKey = GetPrimaryKeyProperty(sourceContext, entityType);

        var targetDict = targetEntities?.ToDictionary(entity => primaryKey.GetValue(entity));

        foreach (var sourceEntity in sourceEntities)
        {
            var primaryKeyValue = primaryKey.GetValue(sourceEntity);

            if (!targetDict.ContainsKey(primaryKeyValue))
            {
                //Insert data
                targetContext.Add(sourceEntity);
                //Call On Added Event
            }
            else
            {
                var targetEntity = targetDict[primaryKeyValue];
                if (!AreEntitiesEqual(sourceEntity, targetEntity))
                {
                    CopyEntityValues(sourceEntity, targetEntity, primaryKey);
                    targetContext.Update(targetEntity);
                    
                    //OnUpdateEvent
                }

                targetDict.Remove(primaryKeyValue);
            }
        }
        foreach (var targetEntity in targetDict.Values)
        {
            targetContext.Remove(targetEntity);
        }
        
    }

    private List<object> GetEntityList(object dbSet)
    {
        var entityType = dbSet.GetType().GetGenericArguments().First();

        var asEnumerableMethod = typeof(Enumerable)
            .GetMethod("AsEnumerable", BindingFlags.Static | BindingFlags.Public)
            ?.MakeGenericMethod(entityType);

        var enumerable = asEnumerableMethod?.Invoke(null, new[] { dbSet });

        var toListMethod = typeof(Enumerable)
            .GetMethod("ToList", BindingFlags.Static | BindingFlags.Public)
            ?.MakeGenericMethod(entityType);

        var list = toListMethod?.Invoke(null, new[] { enumerable });

        return ((IEnumerable<object>)list)?.Cast<object>().ToList() ?? new List<object>();
    }


    private PropertyInfo? GetPrimaryKeyProperty(DbContext context, Type entityType)
    {
        var entityTypeInfo = context.Model.FindEntityType(entityType);

        var primaryKeyName = entityTypeInfo?.FindPrimaryKey()?.Properties.Select(x => x.Name).Single();

        return entityType.GetProperty(primaryKeyName ?? string.Empty);
    }


    private bool AreEntitiesEqual(object sourceEntity, object targetEntity)
    {
        var properties = sourceEntity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var sourceValue = property.GetValue(sourceEntity);
            var targetValue = property.GetValue(targetEntity);

            if (!Equals(sourceValue, targetValue)) return false;
        }
        return true;
    }

    private void CopyEntityValues(object sourceEntity, object targetEntity, PropertyInfo? primaryKey)
    {
        var properties = sourceEntity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p != primaryKey);

        foreach (var property in properties)
        {
            var sourceValue = property.GetValue(sourceEntity);
            property.SetValue(targetEntity, sourceValue);
        }
    }
}