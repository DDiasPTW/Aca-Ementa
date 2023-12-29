using System;
using System.IO;
using UnityEngine;

public static class DatabaseBackup
{
    public static void CreateBackupIfNeeded()
    {
        string databasePath = Path.Combine(Application.persistentDataPath, "dishes.db");
        string backupPath = Path.Combine(Application.persistentDataPath, $"db-backup-{DateTime.Now:yyyy-MM-dd}.db");

        if (!File.Exists(backupPath))
        {
            File.Copy(databasePath, backupPath, overwrite: true);
        }
    }
}
