using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Migrations;
using Microsoft.SharePoint;
using System.Collections.Specialized;

namespace GTMigrations.Helpers
{
    public static class SPFieldHelper
    {
        public static void InsertFormulaFieldToList(string fieldName, string displayName, SPList list, string formula, SPFieldType type, SPNumberFormatTypes format)
        {
            if (list.Fields.ContainsField(fieldName))
            {
                list.Fields.Delete(fieldName);
            }

            string temp = list.Fields.Add(displayName, SPFieldType.Calculated, false);
            SPFieldCalculated f = (SPFieldCalculated)list.Fields[temp];
            f.Formula = formula;
            f.OutputType = type;
            f.DisplayFormat = format;
            f.Update();
            list.Update();
        }

        public static void InsertTextFieldToList(string fieldName, SPList list)
        {
            if (list.Fields.ContainsField(fieldName))
            {
                list.Fields.Delete(fieldName);
            }

            SPField f = list.Fields.CreateNewField(SPFieldType.Text.ToString(), fieldName);
            list.Fields.Add(f);
        }

        public static void InsertDateFieldToList(string fieldName, string displayName, SPList list)
        {
            if (list.Fields.ContainsField(fieldName))
            {
                list.Fields.Delete(fieldName);
            }

            SPFieldDateTime textField = (SPFieldDateTime)list.Fields.CreateNewField(SPFieldType.DateTime.ToString(), fieldName);
            textField.Title = displayName;
            list.Fields.Add(textField);

            list.Update();
        }

        public static void DeleteField(string fieldName, SPList list)
        {
            if (list.Fields.ContainsField(fieldName))
            {
                list.Fields.Delete(fieldName);
            }

            list.Update();
        }

        public static void DeleteLookupField(string lookupList, SPList list)
        {
            for (int i = 0; i < list.Fields.Count; i++)
            {
                SPField field = list.Fields[i];
                if (field.Title.Contains(lookupList + ":"))
                {
                    list.Fields.Delete(field.Title);
                    i--;
                }
            }
 
            list.Update();
        }

        public static void InsertLookupField(string lookupName, SPList list, string lookupList, string lookupFieldName, SPWeb web)
        {
            DeleteLookupField(lookupName, list);
            DeleteLookupField(lookupList, list);
            DeleteField(lookupName, list);

            SPFieldLookup lookupField = (SPFieldLookup)list.Fields.CreateNewField(SPFieldType.Lookup.ToString(), lookupName);
            lookupField.LookupList = web.Lists[lookupList].ID.ToString();
            lookupField.LookupField = lookupFieldName;
            list.Fields.Add(lookupField);

            list.Update();
        }

        public static void InsertDependentFields(string primaryLookupName, SPList list, string relatedList, SPWeb web, string lookupColumn)
        {
            if (list.Fields.ContainsField(primaryLookupName + ":" + lookupColumn))
            {
                list.Fields.Delete(primaryLookupName + ":" + lookupColumn);
            }

            SPFieldLookup lookupField = (SPFieldLookup)list.Fields.GetField(primaryLookupName);

            string secondaryColumn = list.Fields.AddDependentLookup(primaryLookupName + ":" + lookupColumn, lookupField.Id);
            SPFieldLookup newDependencyLookupField = (SPFieldLookup)list.Fields.GetFieldByInternalName(secondaryColumn);
            newDependencyLookupField.LookupField = web.Lists.TryGetList(relatedList).Fields[lookupColumn].InternalName;
            newDependencyLookupField.Update();

            list.Update();
        }

        public static void MakeColumnMultiSelect(string fieldName, SPList list)
        {
            if (list.Fields.ContainsField(fieldName))
            {
                SPFieldLookup field = (SPFieldLookup)list.Fields.GetField(fieldName);
                field.AllowMultipleValues = true;
                field.Update();
            }
        }

        public static void MakeTextColumnPlainText(string fieldName, SPList list)
        {
            if(list.Fields.ContainsField(fieldName))
            {
                SPFieldMultiLineText field = (SPFieldMultiLineText)list.Fields.GetField(fieldName);
                field.RichText = false;
                field.Update();
            }
        }
    }
}
