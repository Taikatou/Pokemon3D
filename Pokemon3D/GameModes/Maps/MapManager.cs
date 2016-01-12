﻿using Pokemon3D.DataModel.GameMode.Map;
using Pokemon3D.FileSystem.Requests;

namespace Pokemon3D.GameModes.Maps
{
    class MapManager : DataRequestModelManager<MapModel>
    {
        public MapManager(GameMode gameMode) : base(gameMode)
        { }

        private string CreateKey(string dataPath)
        {
            return _gameMode.GetMapFilePath(dataPath);
        }

        public override DataModelRequest<MapModel> CreateDataRequest(string dataPath)
        {
            return base.CreateDataRequest(CreateKey(dataPath));
        }
    }
}
