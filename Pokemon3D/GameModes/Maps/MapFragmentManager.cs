﻿using Pokemon3D.DataModel.Json.GameMode.Map;
using Pokemon3D.FileSystem;

namespace Pokemon3D.GameModes.Maps
{
    class MapFragmentManager : DataRequestModelManager<MapFragmentModel>
    {
        public MapFragmentManager(GameMode gameMode) : base(gameMode)
        { }

        private string CreateKey(string dataPath)
        {
            return _gameMode.GetMapFragmentFilePath(dataPath);
        }

        public override DataModelRequest<MapFragmentModel> CreateDataRequest(string dataPath)
        {
            return base.CreateDataRequest(CreateKey(dataPath));
        }
    }
}
