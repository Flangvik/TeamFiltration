using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.SharePoint
{


    public class GetRelevant
    {
        public string odatametadata { get; set; }
        public int ElapsedTime { get; set; }
        public Primaryqueryresult PrimaryQueryResult { get; set; }
        public List<Property2> Properties { get; set; }
        public List<object> SecondaryQueryResults { get; set; }
        public string SpellingSuggestion { get; set; }
        public List<object> TriggeredRules { get; set; }
    }

    public class Primaryqueryresult
    {
        public List<object> CustomResults { get; set; }
        public string QueryId { get; set; }
        public string QueryRuleId { get; set; }
        public object RefinementResults { get; set; }
        public Relevantresults RelevantResults { get; set; }
        public object SpecialTermResults { get; set; }
    }

    public class Relevantresults
    {
        public object GroupTemplateId { get; set; }
        public object ItemTemplateId { get; set; }
        public List<Property1> Properties { get; set; }
        public object ResultTitle { get; set; }
        public object ResultTitleUrl { get; set; }
        public int RowCount { get; set; }
        public Table Table { get; set; }
        public int TotalRows { get; set; }
        public int TotalRowsIncludingDuplicates { get; set; }
    }

    public class Table
    {
        public List<Row> Rows { get; set; }
    }

    public class Row
    {
        public List<Cell> Cells { get; set; }
    }

    public class Cell
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public string ValueType { get; set; }
    }

    public class Property1
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }
    }

    public class Property2
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }
    }


}
