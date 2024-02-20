using DevExpress.Data.Filtering.Helpers;
using DevExpress.Xpf.Editors.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKalkulation.Model
{
    public class CostItem
    {
        public Product Product { get; set; }
        public string Name { get; set; }
        public CostType CostType { get; set; }
        public Resource Resource { get; set; }
        public Tool Tool { get; set; }

        /// <summary>
        /// Teiligkeit, Kastennteil usw.
        /// </summary>
        public double? ToolParting { get; set; }

        public int? QuantityPerTool { get; set; }
        public double? Effort { get; set; }
        public double Factor { get; set; } = 1;
        public double Divior { get; set; } = 1;

        /// <summary>
        /// Alternativer Arbeitsgang
        /// </summary>
        public bool Inactive { get; set; }

        public double CostResult => CalculateCostResult();

        public double TotalCosts => CostResult * Product.AnnualQuantity;

        double CalculateCostResult()
        {
            double? result = 0;
            if (Resource != null)
            {
                result = Resource.CostRatio * Factor;
                if (!(Effort is null))
                {
                    result *= Effort;
                }
                if (!(ToolParting is null))
                {
                    result *= ToolParting;
                }
                if (!(QuantityPerTool is null))
                {
                    result /= QuantityPerTool;
                }
                if (CostType.Id == 2)
                {
                    // Rüstkosten durch Losgröße teilen
                    result /= Product.ProductionQuantity;
                }
            }

            return Math.Round((double)result, 2);
        }
    }

}
