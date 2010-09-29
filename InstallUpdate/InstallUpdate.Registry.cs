﻿using System;
using System.Collections.Generic;
using System.IO;
using wyUpdate.Common;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        void UpdateRegistry(List<RegChange> rollbackRegistry)
        {
            int i = 0;

            ThreadHelper.ReportProgress(Sender, SenderDelegate, string.Empty, GetRelativeProgess(5, 0), 0);

            foreach (RegChange change in UpdtDetails.RegistryModifications)
            {
                if (IsCancelled())
                    break;

                i++;
                int unweightedProgress =  (i * 100) / UpdtDetails.RegistryModifications.Count;

                ThreadHelper.ReportProgress(Sender, SenderDelegate,
                    change.ToString(),
                    GetRelativeProgess(5, unweightedProgress), unweightedProgress);

                //execute the regChange, while storing the opposite operation
                change.ExecuteOperation(rollbackRegistry);
            }
        }

        public void RunUpdateRegistry()
        {
            List<RegChange> rollbackRegistry = new List<RegChange>();

            //parse variables in the regChanges
            for (int i = 0; i < UpdtDetails.RegistryModifications.Count; i++)
                UpdtDetails.RegistryModifications[i] = ParseRegChange(UpdtDetails.RegistryModifications[i]);

            Exception except = null;
            try
            {
                UpdateRegistry(rollbackRegistry);
            }
            catch (Exception ex)
            {
                except = ex;
            }

            RollbackUpdate.WriteRollbackRegistry(Path.Combine(TempDirectory, "backup\\regList.bak"), rollbackRegistry);

            if (canceled || except != null)
            {
                // rollback the registry
                ThreadHelper.ChangeRollback(Sender, RollbackDelegate, true);
                RollbackUpdate.RollbackRegistry(TempDirectory);

                // rollback files
                ThreadHelper.ChangeRollback(Sender, RollbackDelegate, false);
                RollbackUpdate.RollbackFiles(TempDirectory, ProgramDirectory);

                // rollback unregged COM
                RollbackUpdate.RollbackUnregedCOM(TempDirectory);

                // rollback stopped services
                RollbackUpdate.RollbackStoppedServices(TempDirectory);

                ThreadHelper.ReportError(Sender, SenderDelegate, string.Empty, except);
            }
            else
            {
                //registry modification completed sucessfully
                ThreadHelper.ReportSuccess(Sender, SenderDelegate, string.Empty);
            }
        }
    }
}