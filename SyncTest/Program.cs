using CustomSyncLibrary;
using SyncTest;

using var sourceContext = new SourceContext();
using var targetContext = new TargetContext();

var syncService = new FullTableSyncService();

syncService.SyncDatabases(sourceContext, targetContext);

