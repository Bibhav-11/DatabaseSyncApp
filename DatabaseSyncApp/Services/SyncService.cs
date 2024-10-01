using System.Reflection;
using CustomSyncLibrary;
using DatabaseSyncApp.Context;
using DatabaseSyncApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseSyncApp.Services;

public class SyncService
{
    private readonly List<Log> _logs = new List<Log>();
    public void SyncDatabases()
    {
        using var sourceContext = new SourceDbContext();
        using var destinationContext = new DestinationDbContext();

        var syncService = new FullTableSyncService();
        
        syncService.SyncDatabases(sourceContext, destinationContext);
    }

    public async Task SyncCustomerAndLocationDatabases()
    {
        using var sourceContext = new SourceDbContext();
        using var targetContext = new DestinationDbContext();
        
        var sourceCustomers = sourceContext.Set<Customer>()
            .Include(c => c.Locations)
            .AsNoTracking()
            .ToList();

        var targetCustomers = targetContext.Set<Customer>()
            .Include(c => c.Locations)
            .ToList();

        var primaryKey = GetPrimaryKeyProperty(sourceContext, typeof(Customer));

        foreach (var sourceCustomer in sourceCustomers)
        {
            var primaryKeyValue = primaryKey.GetValue(sourceCustomer);

            if (!targetCustomers.Any(x => primaryKey.GetValue(x).Equals(primaryKeyValue)))
            {
                targetContext.Add(sourceCustomer);
                _logs.Add(new Log()
                {
                    TimeStamp = DateTime.UtcNow,
                    ChangedField = sourceCustomer.GetType().Name,
                    Description = $"Added {sourceCustomer.GetType().Name} with Id: {sourceCustomer.CustomerId}, Name: {sourceCustomer.Name}, Email: {sourceCustomer.Email}, Phone: {sourceCustomer.Phone} " +
                                  $"{(sourceCustomer.Locations.Any() ? $"with {sourceCustomer.Locations.Count()} " + 
                                                                       (sourceCustomer.Locations.Count() == 1 ? "location" : "locations") : "")}"
                });

                if (sourceCustomer.Locations.Any())
                {
                    foreach (var sourceCustomerLocation in sourceCustomer.Locations)
                    {
                        _logs.Add(new Log()
                        {
                            TimeStamp = DateTime.UtcNow,
                            ChangedField = sourceCustomerLocation.GetType().Name,
                            Description = $"Added {sourceCustomerLocation.GetType().Name} with Id: {sourceCustomerLocation.LocationId}, Address: {sourceCustomerLocation.Address}"
                        });                    
                    }
                }
            }
            else
            {
                var targetCustomer = targetCustomers.First(x => primaryKey.GetValue(x).Equals(primaryKeyValue));

                if (!AreEntitiesEqual(sourceCustomer, targetCustomer))
                {
                    CopyEntityValues(sourceCustomer, targetCustomer, primaryKey);
                    targetContext.Update(targetCustomer);
                }
                SyncLocations(sourceCustomer.Locations, targetCustomer.Locations, targetContext);
            }
        }

        var deletedCustomers = targetCustomers.Where(x => !sourceCustomers.Any(y => y.CustomerId == x.CustomerId));
        if (deletedCustomers.Any())
        {
            foreach (var deletedCustomer in deletedCustomers)
            {
                _logs.Add(new Log()
                {
                    TimeStamp = DateTime.UtcNow,
                    ChangedField = deletedCustomer.GetType().Name,
                    Description = $"Deleted {deletedCustomer.GetType().Name} with Id: {deletedCustomer.CustomerId} " +
                                  $"{(deletedCustomer.Locations.Any() ? $"with {deletedCustomer.Locations.Count()} " + 
                                                                        (deletedCustomer.Locations.Count() == 1 ? "location" : "locations") : "")}"
                });

                if (deletedCustomer.Locations.Any())
                {
                    foreach (var deletedCustomerLocation in deletedCustomer.Locations)
                    {
                        _logs.Add(new Log()
                        {
                            TimeStamp = DateTime.UtcNow,
                            ChangedField = deletedCustomerLocation.GetType().Name,
                            Description = $"Deleted {deletedCustomerLocation.GetType().Name} with Id: {deletedCustomerLocation.LocationId}"
                        });                    
                    }
                }
            }
        }
        targetContext.RemoveRange(deletedCustomers);


        var deletedLocationsOnly = targetCustomers.Where(tc => sourceCustomers.Any(sc => sc.CustomerId == tc.CustomerId)).SelectMany(x => x.Locations).Where(tl => !sourceCustomers.SelectMany(x => x.Locations).Any(x => x.LocationId == tl.LocationId));

        if (deletedLocationsOnly.Any())
        {
            foreach (var deletedLocation in deletedLocationsOnly)
            {
                _logs.Add(new Log()
                {
                    TimeStamp = DateTime.UtcNow,
                    ChangedField = deletedLocation.GetType().Name,
                    Description = $"Deleted {deletedLocation.GetType().Name} with Id: {deletedLocation.LocationId}"
                });
            }
            targetContext.RemoveRange(deletedLocationsOnly);
        }


        targetContext.AddRange(_logs);
        await targetContext.SaveChangesAsync();
    }

    private void SyncLocations(ICollection<Location> sourceLocations, ICollection<Location> targetLocations, DbContext targetContext)
    {
        foreach (var sourceLocation in sourceLocations)
        {
            if (!targetLocations.Any(tl => tl.LocationId == sourceLocation.LocationId))
            {
                _logs.Add(new Log()
                {
                    TimeStamp = DateTime.UtcNow,
                    ChangedField = sourceLocation.GetType().Name,
                    Description = $"Added {sourceLocation.GetType().Name} with Id: {sourceLocation.LocationId}, Address: {sourceLocation.Address}"
                });
                targetContext.Add(sourceLocation);
            }
            else
            {
                var targetLocation = targetLocations.FirstOrDefault(x => x.LocationId == sourceLocation.LocationId);
                if (!AreEntitiesEqual(sourceLocation, targetLocation))
                {
                    CopyEntityValues(sourceLocation, targetLocation, sourceLocation.GetType().GetProperty(nameof(sourceLocation.LocationId)));
                    targetContext.Update(targetLocation); 
                }
            }
        }
    }

    private PropertyInfo? GetPrimaryKeyProperty(DbContext context, Type entityType)
    {
        var entityTypeInfo = context.Model.FindEntityType(entityType);
        var primaryKey = entityTypeInfo?.FindPrimaryKey();

        if (primaryKey is null) return null;
        return entityType.GetProperty(primaryKey.Properties.First().Name);
    }

    private bool AreEntitiesEqual(object sourceEntity, object targetEntity)
    {
        var entityType = sourceEntity.GetType();

        foreach (var property in entityType.GetProperties())
        {
            if (!property.CanRead || !property.CanWrite) continue;

            var sourceValue = property.GetValue(sourceEntity);
            var targetValue = property.GetValue(targetEntity);

            if (sourceValue.Equals(targetValue)) continue;
            else
            {
                return false;
            }
        }
        return true;
    }

    private void CopyEntityValues(object sourceEntity, object targetEntity, PropertyInfo primaryKey)
    {
        var entityType = sourceEntity.GetType();

        foreach (var property in entityType.GetProperties())
        {
            if (!property.CanRead || !property.CanWrite) continue;
            if (property.Name == primaryKey.Name) continue;

            if (!property.GetValue(sourceEntity).Equals(property.GetValue(targetEntity)))
            {
                string propertyName = property.Name;
                string primaryKeyString = $"{primaryKey.Name} {primaryKey.GetValue(sourceEntity)}";
                string previousValue = property.GetValue(targetEntity).ToString();
                string newValue = property.GetValue(sourceEntity).ToString();
                _logs.Add(new Log()
                {
                    TimeStamp = DateTime.UtcNow,
                    ChangedField = propertyName,
                    PreviousValue = previousValue,
                    NewValue = newValue,
                    Description = $"{primaryKeyString}: {propertyName} changed from {previousValue} to {newValue}"
                });
                property.SetValue(targetEntity, property.GetValue(sourceEntity));
            }
        }
    }
}