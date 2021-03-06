﻿using Pokemon3D.Common.Diagnostics;
using Pokemon3D.DataModel;
using Pokemon3D.DataModel.i18n;
using Pokemon3D.FileSystem;
using Pokemon3D.GameCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pokemon3D.UI.Localization
{
    class CoreTranslationManager : TranslationManager
    {
        public CoreTranslationManager()
        {
            GameController.Instance.GetService<GameConfiguration>().ConfigFileLoaded += OnConfigFileLoaded;

            if (Directory.Exists(i18nPathProvider.LookupPath))
            {
                List<SectionModel> sectionModels = new List<SectionModel>();

                var files = Directory.GetFiles(i18nPathProvider.LookupPath).
                    Where(m => m.EndsWith(I18NFileExtension, StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    try
                    {
                        sectionModels.AddRange(DataModel<SectionModel[]>.FromFile(file));
                    }
                    catch (DataLoadException)
                    {
                        GameLogger.Instance.Log(MessageType.Error, "Error trying to load internationalization file (" + file + ").");
                    }
                }

                Load(sectionModels.ToArray());
            }
            else
            {
                GameLogger.Instance.Log(MessageType.Warning, "The internationalization folder (\"i18n\") was not found.");
            }
        }

        private void OnConfigFileLoaded(object sender, EventArgs e)
        {
            OnLanguageChanged(sender, e);
        }
    }
}
