﻿namespace Assets.Scripts.DataPersistence.BlankBlockComponent
{
    public interface IUpToConfigTemplateService
    {
        string[] GetSchemaTemplate(string dbConn, string table);
        string[] GetTablesTemplate(string dbConn);
        string[] GetAttributesTemplate(string dbConn, string table);
    }
}
