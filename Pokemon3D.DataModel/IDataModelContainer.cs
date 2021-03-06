﻿namespace Pokemon3D.DataModel
{
    /// <summary>
    /// An object containing a DataModel.
    /// </summary>
    public interface IDataModelContainer
    {
        /// <summary>
        /// Returns if the container loaded the data correctly.
        /// </summary>
        bool IsValid { get; }
    }
}
