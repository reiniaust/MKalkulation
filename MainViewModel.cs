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
using System.Windows;

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

        public List<Resource> Ressources { get; set; }

        public MainViewModel() {


            List<Debitor> debitorTbl = GetDAO<Debitor>().GetTable().ToList();
            List<Teile> teileTbl = GetDAO<Teile>().GetTable().Where(t => t.TeilGeloescht == 0 && t.KalkLos > 0 && t.FoAnlage > 0
                //&& t.tbJahresBedarf > 0 && t.AendStandDatum >= DateTime.Today.AddYears(-5)) 
                && t.TeileNr == 103211192)
                .ToList();
            List<I_Schmelzgruppen> schmelzGrpTbl = GetDAO<I_Schmelzgruppen>().GetTable().ToList();
            List<Werkstoffe> tblWerkstoffe = GetDAO<Werkstoffe>().GetTable().ToList();
            List<Material> tblMaterial = GetDAO<Material>().GetTable().ToList();
            List<MatFzWe> tblMatFzWe = GetDAO<MatFzWe>().GetTable().ToList();
            List<Mod_Platt> tblModPlatt = GetDAO<Mod_Platt>().GetTable().ToList();
            List<ModPlattHilfsstoffe> tblModPlattHilfsstoffe = GetDAO<ModPlattHilfsstoffe>().GetTable().ToList();

            List<Department> departments = new List<Department>();
            departments.Add(new Department() { Id = 1, Name = "01 Metall" });
            departments.Add(new Department() { Id = 2, Name = "02 Kerne" });
            departments.Add(new Department() { Id = 3, Name = "03 Hilfsstoffe" });
            departments.Add(new Department() { Id = 4, Name = "04 Formen/Gießen" });

            List<CostType> costTypes = new List<CostType>();
            costTypes.Add(new CostType() { Id = 1, Name = "01 Material" });
            costTypes.Add(new CostType() { Id = 2, Name = "02 Rüsten" });
            costTypes.Add(new CostType() { Id = 3, Name = "03 Fertigung" });

            Ressources = new List<Resource>();

            // Schmelzgruppen
            schmelzGrpTbl.ForEach(i =>
            {
                Ressources.Add(new Resource() { Id = i.Nummer, Name = i.Material, Department = departments[0], CostType = costTypes[2], CostRatio = Math.Round((double)(i.SchmKostVar + i.SchmKostFix), 3) });
            });

            // Werkstoffe
            tblWerkstoffe.Where(w => w.WerkstoffID != 0).ToList().ForEach(w =>
            {
                Ressources.Add(new Resource() { Id = w.WerkstoffID, Name = w.WerkstoffBez, Department = departments[0],
                    CostType = costTypes[0], CostRatio = Math.Round((double)w.FesterKostensatz, 3) });
            });

            // Kern-Sandarten
            GetDAO<I_Sandarten>().GetTable().Where(i => i.Nummer != 0).ToList().ForEach(i =>
            {
                Ressources.Add(new Resource() { Id = i.Nummer, Name = i.Sandart, Department = departments[1], CostType = costTypes[0], CostRatio = Math.Round((double)(i.SandKostenVar + i.SandKostenFix), 3) });
            });

            // Kernmacherei
            GetDAO<I_KernAnlagen>().GetTable().Where(i => i.Nummer != 0).ToList().ForEach(i =>
            {
                Ressources.Add(new Resource() { Id = i.Nummer, Name = i.KernAnlBez, Department = departments[1], CostRatio = Math.Round((double)(i.FertigKostVar + i.FertigKostFix), 3), SettingUpTime = (double)i.RuestZeit });
                if (i.NebenKostVar > 0) 
                {
                    Ressources.Add(new Resource() { Id = i.Nummer, Name = "Nebenzeit", Department = departments[1], CostRatio = Math.Round((double)(i.NebenKostVar + i.FertigKostFix), 3) });
                }
            });

            // Hilfsstoffe
            I_Matgruppe hilfsMatGrp = GetDAO<I_Matgruppe>().GetTable().ToList().Find(g => g.ZuordnungsID == 3);
            GetDAO<I_Hilfstoffe>().GetTable().Where(i => i.NichtKalkDruck == false).ToList().ForEach(i =>
            {
                double? kostensatz = i.Kostensatz;
                if (hilfsMatGrp != null)
                {
                    Material material = tblMaterial.Find(m => m.MaterialGruppe == hilfsMatGrp.MatgruppeID && m.VerknuepfNr == i.Nummer);
                    if (material != null) 
                    {
                        MatFzWe matWerte = tblMatFzWe.Find(m => m.MaterialNr == material.MaterialNr);
                        if (matWerte.FzKalkPreis != 0)
                        {
                            kostensatz = matWerte.FzKalkPreis;
                        }
                        else
                        {
                            kostensatz = matWerte.FzDsPreis;
                        }
                    }
                }
                if (kostensatz == null)
                {
                    kostensatz = 0;
                }
                try
                {
                Ressources.Add(new Resource() { Id = i.Nummer, Name = i.Bezeichnung, Department = departments[2], CostRatio = Math.Round((double)(kostensatz), 3) });
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            });

            // Formerei
            GetDAO<I_Formanlagen>().GetTable().Where(i => i.Nummer != 0).ToList().ForEach(i =>
            {
                Ressources.Add(new Resource() { Id = i.Nummer, Name = "Formsand", Department = departments[3], CostType = costTypes[0], CostRatio = (double)(i.SandKostenVar + i.SandKostenFix) });
                Ressources.Add(new Resource() { Id = i.Nummer, Name = i.FormAnlage, Department = departments[3], SettingUpTime = (double)i.RuestZeit, CostRatio = Math.Round((double)(i.FormKostenVar + i.FormKostenFix), 3) });
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
                // in absolate Zahlen umwandeln
                t.GewRoh = Math.Abs((double)t.GewRoh);
                t.GewKreislauf = Math.Abs((float)t.GewKreislauf);

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

                
                int werkstoffId = (int)t.WerkstoffID;
                if (t.WerkstoffInternID != 0)
                {
                    werkstoffId = (int)t.WerkstoffInternID;
                }
                if (werkstoffId != 0)
                {

                    // Metall
                    Resource ressource = Ressources.FirstOrDefault(r => r.Department == departments[0] && r.CostType == costTypes[0] && r.Id == t.WerkstoffID);
                    if (ressource != null)
                    {
                        Werkstoffe werkstoff =tblWerkstoffe.Find(w => w.WerkstoffID == werkstoffId);
                        I_Schmelzgruppen schmelzgruppe = schmelzGrpTbl.Find(s => s.Nummer == werkstoff.Schmelzgruppe);
                        if (schmelzgruppe != null)
                        {
                            double einsatzGew = Math.Round((double)((t.GewRoh + t.GewKreislauf)), 2);
                            CostItems.Add(new CostItem()
                            {
                                Name = "Einsatz",
                                Product = product,
                                Resource = ressource,
                                CostType = ressource.CostType,
                                Effort = einsatzGew,
                                Factor = Math.Round((double)schmelzgruppe.AbbrandFaktor, 3)
                            });
                            CostItems.Add(new CostItem()
                            {
                                Name = "Rückwert",
                                Product = product,
                                Resource = ressource,
                                CostType = ressource.CostType,
                                Effort = Math.Round((double)((-t.GewKreislauf) * werkstoff.Rueckwert / 100), 2)
                            });

                            // Schmelzen
                            ressource = Ressources.FirstOrDefault(r => r.Department == departments[0] && r.CostType == costTypes[2] && r.Id == werkstoff.Schmelzgruppe);
                            CostItems.Add(new CostItem()
                            {
                                Name = "Schmelzen",
                                Product = product,
                                Resource = ressource,
                                CostType = ressource.CostType,
                                Effort = einsatzGew,
                                Factor = Math.Round((double)schmelzgruppe.AbbrandFaktor, 3)
                            });
                        }
                    }


                    if (t.FoAnlage != 0 & t.FoMin != 0)
                    {
                        // Formsand
                        ressource = Ressources.FirstOrDefault(r => r.Department == departments[3] && r.CostType == costTypes[0] && r.Id == t.FoAnlage);
                        CostItems.Add(new CostItem()
                        {
                            Name = "Stoff",
                            Product = product,
                            Resource = ressource,
                            CostType = ressource.CostType,
                            Effort = (double)t.FoSandVol
                        });

                        ressource = Ressources.FirstOrDefault(r => r.Department == departments[3] && r.CostType is null && r.Id == t.FoAnlage);

                        // Formen
                        CostItems.Add(new CostItem()
                        {
                            Name = "Formen/Gießen",
                            Product = product,
                            Resource = ressource,
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
                            Resource = ressource,
                            CostType = costTypes[1],
                            Effort = (double)(ressource.SettingUpTime + t.FoRuestMin)
                        });

                        // Kerne
                        GetDAO<Tei_Kerne>().GetTable().Where(k => k.TeileID == t.TeileID).ToList().ForEach(k =>
                        {
                            // Kernsand
                            ressource = Ressources.FirstOrDefault(r => r.Department == departments[1] && r.CostType == costTypes[0] && r.Id == k.SandArtID);
                            CostItems.Add(new CostItem()
                            {
                                Name = "K " + k.KernNr,
                                Product = product,
                                Resource = ressource,
                                CostType = costTypes[0],
                                Effort = Math.Round((double)(k.KernGew), 2),
                                Factor = (double)k.StueckFuer,
                                Divisor = (double)k.AnzAbguss
                            });

                            ressource = Ressources.FirstOrDefault(r => r.Department == departments[1] && r.CostType is null && r.Id == k.KernAnID);

                            // Kern-Rüstzeit
                            CostItems.Add(new CostItem()
                            {
                                Name = "K " + k.KernNr,
                                Product = product,
                                Resource = ressource,
                                CostType = costTypes[1],
                                Effort = Math.Round((double)(ressource.SettingUpTime), 2)
                            });
  
                            // Kern-Fertigung
                            CostItems.Add(new CostItem()
                            {
                                Name = "K " + k.KernNr,
                                Product = product,
                                Resource = ressource,
                                CostType = costTypes[2],
                                Effort = Math.Round((double)k.KernMin, 2),
                                Factor = (double)k.StueckFuer,
                                Divisor = (double)(k.AnzAbguss * k.StProKK)
                            });

                            if (k.NebenMin != 0)
                            {
                                // Kern-Nebenzeitz
                                ressource = Ressources.FirstOrDefault(r => r.Name == "Nebenzeit" && r.CostType is null && r.Id == k.KernAnID);
                                CostItems.Add(new CostItem()
                                {
                                    Name = "K " + k.KernNr,
                                    Product = product,
                                    Resource = ressource,
                                    CostType = costTypes[2],
                                    Effort = Math.Round((double)k.NebenMin, 2),
                                    Factor = (double)k.StueckFuer,
                                    Divisor = (double)k.AnzAbguss

                                });
                            }
                        });
  
                        // Hilfsstoffe
                        GetDAO<Tei_Hilfsstoffe>().GetTable().Where(h => h.TeileID == t.TeileID).ToList().ForEach(h =>
                        {
                            ressource = Ressources.FirstOrDefault(r => r.Department == departments[2] && r.CostType is null && r.Id == h.HilfsstoffID);

                            if (ressource != null)
                            {
                                // Modellplatte (Werkzeug) zuordnen, wenn ein Hilfsstoff-Datensatz bei einer Modellplatte keien Verwendung findet
                                bool inactive = false;
                                Tool tool = new Tool();
                                ModPlattHilfsstoffe modPlattHilfsstoff = tblModPlattHilfsstoffe.Find(
                                    m => m.IdTyp == 'T' && m.IdentTeilAuftr == h.TeileNr && m.SatzIdHilfstoffe == h.tblSatzID && m.IndexNrMod == 0 && m.Verwendet == false);
                                if (modPlattHilfsstoff != null)
                                {
                                    modPlattHilfsstoff = tblModPlattHilfsstoffe.Find(
                                        m => m.IdTyp == 'T' && m.IdentTeilAuftr == h.TeileNr && m.SatzIdHilfstoffe == h.tblSatzID && m.IndexNrMod == 0 && m.Verwendet == true);
                                    if (modPlattHilfsstoff != null)
                                    {
                                        tool = tools.Find(to => to.Id == modPlattHilfsstoff.BauteilID);
                                        if (tool == null)
                                        {
                                            tool = new Tool() { Id = (int)modPlattHilfsstoff.BauteilID, Name = modPlattHilfsstoff.BauteilName };
                                            tools.Add(tool);
                                        }
                                        if (tblModPlatt.Find(mp => mp.BauteilID == modPlattHilfsstoff.BauteilID).BevEinrichtung == false)
                                        {
                                            inactive = true;
                                        }
                                    }
                                }

                                CostItems.Add(new CostItem()
                                {
                                    Product = product,
                                    Resource = ressource,
                                    Tool = tool,
                                    CostType = costTypes[0],
                                    Factor = (double)h.StueckFuer,
                                    Divisor = (double)h.AnzAbguss,
                                    Inactive = inactive,
                                }); ;
                            }
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
