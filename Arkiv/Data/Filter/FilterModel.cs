namespace Arkiv.Data.Filter
{
    public class FilterModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public FilterValueModel Value { get; set; }

    }

    public class FilterValueModel
    {
        public string One { get; set; }
        public string Two { get; set; }
    }
}