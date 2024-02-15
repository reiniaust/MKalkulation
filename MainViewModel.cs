using MKalkulation.Model;
using System.ComponentModel;
using System.Collections.ObjectModel;
using SQLServer;
using System;
using System.Collections.Generic;
using System.Linq;
using static sdDatabase.DAOManager;
using GIBase;
using DevExpress.Xpf.Editors.Helpers;

namespace MKalkulation
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<CostItem> _costItems;
        public ObservableCollection<CostItem> CostItems
        {
            get { return _costItems; }
            set
            {
                if (_costItems != value)
                {
                    _costItems = value;
                    OnPropertyChanged(nameof(CostItems));
                }
            }
        }
        public ObservableCollection<CostItem> Selection { get; } = new ObservableCollection<CostItem>();

        public List<Resource> Resources { get; set; }

        public MainViewModel() {


            List<Debitor> debitorTbl = GetDAO<Debitor>().GetTable().ToList();
            List<Teile> teileTbl = GetDAO<Teile>().GetTable().Where(t => t.TeilGeloescht == 0 && t.KalkLos > 0 && t.tbJahresBedarf > 0 && t.FoAnlage > 0 && t.AendStandDatum >= DateTime.Today.AddYears(-5)) .ToList();


            List<Department> departments = new List<Department>();
            departments.Add(new Department() { Id = 1, Name = "01 Schmelzbetrieb" });
            departments.Add(new Department() { Id = 2, Name = "02 Kernmacherei" });
            departments.Add(new Department() { Id = 3, Name = "03 Hilfsstoffe" });
            departments.Add(new Department() { Id = 4, Name = "04 Formerei" });

            List<CostType> costTypes = new List<CostType>();
            costTypes.Add(new CostType() { Id = 1, Name = "01 Material" });
            costTypes.Add(new CostType() { Id = 2, Name = "02 Rüsten" });
            costTypes.Add(new CostType() { Id = 3, Name = "03 Fertigung" });

            Resources = new List<Resource>();

            // Schmelzgruppen
            GetDAO<I_Schmelzgruppen>().GetTable().ToList().ForEach(i =>
            {
                Resources.Add(new Resource() { Id = i.Nummer, Name = i.Material, Department = departments[0], CostType = costTypes[2], CostRatio = Math.Round((double)(i.SchmKostVar + i.SchmKostFix), 3) });
            });

            // Werkstoffe
            GetDAO<Werkstoffe>().GetTable().Where(w => w.WerkstoffID != 0).ToList().ForEach(w =>
            {
                Resources.Add(new Resource() { Id = w.WerkstoffID, Name = w.WerkstoffBez, Department = departments[0],
                    CostType = costTypes[0], CostRatio = Math.Round((double)w.FesterKostensatz, 3) });
            });

            // Kern-Sandarten
            GetDAO<I_Sandarten>().GetTable().Where(i => i.Nummer != 0).ToList().ForEach(i =>
            {
                Resources.Add(new Resource() { Id = i.Nummer, Name = i.Sandart, Department = departments[1], CostType = costTypes[0], CostRatio = Math.Round((double)(i.SandKostenVar + i.SandKostenFix), 3) });
            });

            // Kernmacherei
            GetDAO<I_KernAnlagen>().GetTable().Where(i => i.Nummer != 0).ToList().ForEach(i =>
            {
                Resources.Add(new Resource() { Id = i.Nummer, Name = i.KernAnlBez, Department = departments[1], CostRatio = Math.Round((double)(i.FertigKostVar + i.FertigKostFix), 3), SettingUpTime = (double)i.RuestZeit });
            });

            // Hilfsstoffe
            GetDAO<I_Hilfstoffe>().GetTable().Where(i => i.Nummer != 0).ToList().ForEach(i =>
            {
                Resources.Add(new Resource() { Id = i.Nummer, Name = i.Bezeichnung, Department = departments[2], CostRatio = Math.Round((double)(i.Kostensatz), 3) });
            });

            // Formerei
            GetDAO<I_Formanlagen>().GetTable().Where(i => i.Nummer != 0).ToList().ForEach(i =>
            {
                Resources.Add(new Resource() { Id = i.Nummer, Name = "Formsand", Department = departments[3], CostType = costTypes[0], CostRatio = (double)(i.SandKostenVar + i.SandKostenFix) });
                Resources.Add(new Resource() { Id = i.Nummer, Name = i.FormAnlage, Department = departments[3], SettingUpTime = (double)i.RuestZeit, CostRatio = Math.Round((double)(i.FormKostenVar + i.FormKostenFix), 3) });
            });


            List<Tool> tools = new List<Tool>();
            tools.Add(new Tool() { Name = "Modell" });

            // Kunden
            List<Customer> customers = new List<Customer>();
            debitorTbl.ForEach(d =>
            {
                customers.Add(new Customer() { Id = d.KundenNr, Name = d.NameKurz });
            });


            // Teile
            List<Product> products = new List<Product>();
            CostItems = new ObservableCollection<CostItem>();
            teileTbl.ForEach(t =>
            {
                int anualQuant = 0;
                if (!(t.tbJahresBedarf is null))
                {
                    anualQuant = (int)t.tbJahresBedarf;
                }
                Product product = new Product() { 
                    Id = (int)t.TeileNr,
                    Name = t.ModellBez, 
                    ProductNo = t.ModellNr, 
                    Customer = customers.FirstOrDefault(c => c.Id == t.KundenNr),
                    ProductionQuantity = (int)t.KalkLos,
                    AnnualQuantity = anualQuant
                };
                products.Add(product);

                if (t.WerkstoffID != 0)
                {

                    // Metall
                    Resource resource = Resources.FirstOrDefault(r => r.Department == departments[0] && r.CostType == costTypes[0] && r.Id == t.WerkstoffID);

                    if (resource != null)
                    {
                        CostItems.Add(new CostItem()
                        {
                            Name = "Werkstoff",
                            Product = product,
                            Resource = resource,
                            CostType = resource.CostType,
                            Effort = Math.Round((double)t.GewRoh, 3)
                        });

                        // Schmelzen
                        int id = (int)GetDAO<Werkstoffe>().GetTable().FirstOrDefault(w => w.WerkstoffID == t.WerkstoffID).Schmelzgruppe;
                        resource = Resources.FirstOrDefault(r => r.Department == departments[0] && r.CostType == costTypes[2] && r.Id == id);
                        CostItems.Add(new CostItem()
                        {
                            Name = "Schmelzen",
                            Product = product,
                            Resource = resource,
                            CostType = resource.CostType,
                            Effort = Math.Round((double)(t.GewRoh + t.GewKreislauf), 3)
                        });
                    }


                    if (t.FoAnlage != 0 & t.FoMin != 0)
                    {

                        // Formsand
                        resource = Resources.FirstOrDefault(r => r.Department == departments[3] && r.CostType == costTypes[0] && r.Id == t.FoAnlage);
                        CostItems.Add(new CostItem()
                        {
                            Name = "Stoff",
                            Product = product,
                            Resource = resource,
                            CostType = resource.CostType,
                            Effort = (double)t.FoSandVol
                        });

                        resource = Resources.FirstOrDefault(r => r.Department == departments[3] && r.CostType is null && r.Id == t.FoAnlage);

                        // Formen
                        CostItems.Add(new CostItem()
                        {
                            Name = "Formen/Gießen",
                            Product = product,
                            Resource = resource,
                            CostType = costTypes[2],
                            ToolParting = t.KastAnteilZaehl / t.KastAnteilNenn,
                            QuantityPerTool = (int)t.AnzStKast,
                            Effort = 60 / (double)t.FoMin
                        });

                        // Rüsten
                        CostItems.Add(new CostItem()
                        {
                            Name = "Rüsten",
                            Product = product,
                            Resource = resource,
                            CostType = costTypes[1],
                            Effort = (double)(resource.SettingUpTime + t.FoRuestMin)
                        });

                        // Kerne
                        GetDAO<Tei_Kerne>().GetTable().Where(k => k.TeileID == t.TeileID).ToList().ForEach(k =>
                        {
                            // Kernsand
                            resource = Resources.FirstOrDefault(r => r.Department == departments[1] && r.CostType == costTypes[0] && r.Id == k.SandArtID);
                            CostItems.Add(new CostItem()
                            {
                                Name = "K " + k.KernNr,
                                Product = product,
                                Resource = resource,
                                CostType = costTypes[0],
                                Effort = Math.Round((double)(k.KernGew), 3)
                            });

                            resource = Resources.FirstOrDefault(r => r.Department == departments[1] && r.CostType is null && r.Id == k.KernAnID);

                            // Kern-Rüstzeit
                            CostItems.Add(new CostItem()
                            {
                                Name = "K " + k.KernNr,
                                Product = product,
                                Resource = resource,
                                CostType = costTypes[1],
                                Effort = Math.Round((double)(resource.SettingUpTime), 3)
                            });
  
                            // Kern-Fertigung
                            CostItems.Add(new CostItem()
                            {
                                Name = "K " + k.KernNr,
                                Product = product,
                                Resource = resource,
                                CostType = costTypes[2],
                                Effort = Math.Round((double)(k.KernMin), 3)
                            });
                        });
  
                        // Hilfsstoffe
                        GetDAO<Tei_Hilfsstoffe>().GetTable().Where(h => h.TeileID == t.TeileID).ToList().ForEach(h =>
                        {
                            resource = Resources.FirstOrDefault(r => r.Department == departments[2] && r.CostType is null && r.Id == h.HilfsstoffID);

                            CostItems.Add(new CostItem()
                            {
                                Product = product,
                                Resource = resource,
                                CostType = costTypes[0],
                                Effort = Math.Round((double)(h.StueckFuer / h.AnzAbguss), 3)
                            });
                        });
                    }
                }
            });


            foreach (CostItem item in CostItems)
            {
                if (!item.Inactive)
                {
                    Selection.Add(item);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
