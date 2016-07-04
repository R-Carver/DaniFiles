using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;
using WebApplication1.Utilities;
using WebApplication1.Utilities.EventSystem;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class ContractController : Controller
    {
        //Mosaab
        private BlobUtility utility;
        private string accountName = "sopro16";
        private string accountKey = "aP1PEQJM/c4SCeGjNpfcMPGSOALYZcFXAWt0aWO++qiC4wSurWzMAh3AWoQ9bEAz+zEZxDUyrCzfuKXK6A6IVg==";

        private MyDbContext db = new MyDbContext();
        private UserManager<ContractUser> manager;

        //Button for CreateWizard
        private string continueBtn = "Weiter";

        public ContractController()
        {
            manager = new UserManager<ContractUser>(new UserStore<ContractUser>(db));
            //Mosaab: ....Files Blob
            utility = new BlobUtility(accountName, accountKey);

        }
        // GET: Contract
        public ActionResult Index()
        {
            var currentUser = manager.FindById(User.Identity.GetUserId());
            //return View(db.Contracts.ToList());
            //Ober: Displays now Signer and Owner Contracts
            return View(db.Contracts.Where(c => c.Owner.Id == currentUser.Id || c.Signer.Id == currentUser.Id).ToList());
        }

        // GET: Contract/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Contract contract = db.Contracts.Find(id);
            if (contract == null)
            {
                return HttpNotFound();
            }
            return View(contract);
        }

        /********************************Delete************************************/
        // GET: Contract/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Contract contract = db.Contracts.Find(id);
            if (contract == null)
            {
                return HttpNotFound();
            }
            return View(contract);
        }

        // POST: Contract/DeleteConfirmed
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Contract contract = db.Contracts.Find(id);
            db.Contracts.Remove(contract);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // POST: Contract/TrashConfirmed
        [ActionName("Trash")]
        [ValidateAntiForgeryToken]
        public ActionResult TrashConfirmed(int id)
        {
            Contract contract = db.Contracts.Find(id);
            contract.ContractStatus = db.ContractStatuses.Find("deleted");
            db.Entry(contract).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //*********************************************************************************************************************************
        //GET: CreateInit
        public ActionResult CreateInit(int? id)
        {
            //id is required when in Edit mode
            ContractCreateInitViewModel model = new ContractCreateInitViewModel();
            if (id != null)
            {
                model.ContractId = (int)id;
            }
            model = CreateInitHelper(model);
            return View(model);
        }

        // POST: Contract/CreateInit
        [HttpPost]
        public ActionResult CreateInit(ContractCreateInitViewModel model, string submit)
        {
            if (ModelState.IsValid)
            {
                //Initialize new contract 
                Contract contract = new Contract();   
                if (model.ContractId != null)
                {
                    //this only if contract is edited - load contract from db
                    var c = db.Contracts.Find(model.ContractId);
                    if (c != null)
                    {
                        contract = c;
                    }
                }
                else
                {
                    //first creation of the contract - Signer and owner are not to be changed
                    contract.SignerId = User.Identity.GetUserId();
                    contract.OwnerId = model.OwnerId;
                }

                contract.Description = model.Description;

                //Set Contract Status
                contract.ContractStatus = HelperUtility.checkContractStatus(contract, db);
                //Add Contract to View to display its information in the RightFormPartial (for Status etc...)
                model.Contract = contract;

                if (contract.Id == 0) //Means that contract isn't in DB
                {
                    db.Contracts.Add(contract);
                }
                else
                {
                    db.Entry(contract).State = EntityState.Modified;
                }
                db.SaveChanges();

                //DAVID TaskTest *************************************************************************************

                contract.TriggerDispatcherTaskEvent();

                /*string tempUserId = contract.OwnerId;  //Get the user from the passed contract obejct
                var TempUser = db.Users.Find(tempUserId);

                //contract = db.Contracts.Find(contract.Id);

               

                var tempTask = new ContractTask();

                tempTask.Description = "tolle Aufgabe";
                tempTask.TaskType = TaskTypes.DispatcherZuweisen;
                tempTask.Contract = contract;
                tempTask.User = TempUser;

                db.ContractTasks.Add(tempTask);
                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine("Aufgabe erstellt");*/

                //DAVID TaskTest *********************************************************************************ENDE



                //Decide which button was pressed...then redirect
                if (submit == continueBtn)
                {
                    return RedirectToAction("CreateGeneral", new { id = contract.Id });
                }
                else
                {
                    return RedirectToAction("Index");
                }

            }

            //Repeat Model Initialization of SelectLists -> See GET: ActionMethod
            model = CreateInitHelper(model);
            //initialization:end

            return View(model);

        }

        //CreateInitHelper
        public ContractCreateInitViewModel CreateInitHelper(ContractCreateInitViewModel model)
        {
            //Get User for SelectLists later
            var userId = User.Identity.GetUserId();
            var currentUser = manager.FindById(userId);
            string currentName = currentUser.UserName.ToString();

            //If Contract already exists
            if (model.ContractId != null)
            {
                Contract contract = db.Contracts.Find(model.ContractId);
                if (contract != null)
                {   //set required attributes
                    model.OwnerId = contract.OwnerId;
                    model.Description = contract.Description;
                    model.SignerName = contract.Signer.UserName;
                }
            }
            else
            {
                //set Signer to current User
                model.SignerName = currentUser.UserName;

                //Put default value for Owner in the form
                model.OwnerId = userId;
            }


            //Get SelectLists for Dropdown initialize with default User
            model.OwnerList = new SelectList(new[] { manager.FindById(userId) }, "Id", "UserName", model.OwnerId);

            //Initializes the ClientList with all existent Clients
            model.ClientList = new SelectList(db.Clients, "Id", "ClientName");

            //Get Department from User
            Department currentDepartment = QueryUtility.GetDepartmentsOfUser(currentName, db).FirstOrDefault();

            //Get Client off the current User for the signer-Dropdown
            string currentClientName;
            if (currentDepartment != null)
            {
                //Saves the Client of Department of current User in currentClient
                IQueryable<Client> currentClient = QueryUtility.GetClientOfDepartment(currentDepartment.DepartmentName, db);
                currentClientName = currentClient.Select(c => c.ClientName).FirstOrDefault();

                if (currentClientName == null)
                {
                    currentClientName = model.ClientList.FirstOrDefault().Text;
                }
            }
            else
            {
                //this case is only relevent for Users without a department (should only be the Admin)
                currentClientName = model.ClientList.FirstOrDefault().Text;
                currentDepartment = db.Departments.FirstOrDefault();
            }

            //Get the Department from the clients for the Owner-DropDown
            model.DepartmentList = new SelectList(QueryUtility.GetDepartmentsFromClient(currentClientName, db), "Id", "DepartmentName", currentDepartment.Id);

            return model;
        }

        //*********************************************************************************************************************************
        // GET: Contract/Create/CreateGeneral
        public ActionResult CreateGeneral(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //David Create and populate the ViewModel
            var model = new ContractCreateGeneralViewModel();
            //Set the Contract Id !!before initialization with Helper
            model.ContractId = (int)id;
            model = CreateGeneralHelper(model);

            return View(model);
        }

        [HttpPost]
        public ActionResult CreateGeneral(ContractCreateGeneralViewModel model, string submit)
        {
            if (ModelState.IsValid) //If all values are accepted
            {
                //load contract
                var contract = db.Contracts.Find(model.ContractId);

                //set contract from model
                db.Entry(contract).Reference(c => c.ContractKind).Load(); //Must loaded before setting to null
                contract.ContractKind = db.ContractKinds.Find(model.ContractKindId);
                db.Entry(contract).Reference(c => c.ContractType).Load();
                contract.ContractType = db.ContractTypes.Find(model.ContractTypeId);
                db.Entry(contract).Reference(c => c.ContractSubType).Load();
                contract.ContractSubType = db.ContractSubTypes.Find(model.ContractSubTypeId);
                contract.DepartmentId = model.DepartmentId;
                contract.SupervisorDepartmentId = model.SupervisorDepartmentId;
                contract.Remarks = model.Remarks;
                var docAdress = new PhysicalDocAddress();
                docAdress.Department = db.Departments.Find(model.PDA_DepartmentId);
                docAdress.Room = model.PDA_Room;
                docAdress.Address = model.PDA_Adress;
                contract.PhysicalDocAddress = docAdress;
                db.Entry(contract).Reference(c => c.ContractPartner).Load();
                contract.ContractPartner = db.ContractPartners.Find(model.ContractPartnerId);
                contract.ExtContractNum = model.ExtContractNum;

                //DAVID TaskTest *************************************************************************************

                //Dispatcher wird noch nicht gesetzt
                var userId = User.Identity.GetUserId();
                var currentUser = manager.FindById(userId);

                contract.Dispatcher = currentUser;

                //Mark the Task as done and schedule deleting
                contract.TriggerDispatcherSetEvent();

                //Generate the Task to Complete the Contract Information for the Dispatcher
                contract.TriggerContractToBeCompletedEvent();

                //Generate the Task to Add the Contract File 
                contract.TriggerFilesToBeAddedEvent();

                //Mark the Task as done and schedule deleting
                contract.TriggerContractCompletedEvent();  //this is not the right place

                //Mark the Task as done and schedule deleting
                contract.TriggerFilesAddedEvent();

                //DAVID TaskTest *********************************************************************************ENDE

                //FrameContract
                switch (model.FrameOptionChosen)
                {
                    case "FrameMain":
                        contract.IsFrameContract = true;
                        break;
                    case "FrameSub":
                        /*contract.IsFrameContract = false;*/
                        if (model.MainFrameIdSelected != null)
                        {
                            var frame = db.Contracts.Find(model.MainFrameIdSelected);
                            contract.FrameContract = frame;
                        }
                        break;
                    //case "NoFrame":
                    default:
                        contract.IsFrameContract = false;
                        contract.FrameContractId = null;
                        break;
                }

                //Set Contract Status
                contract.ContractStatus = HelperUtility.checkContractStatus(contract, db);

                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();

                //Decide which button was pressed...then redirect
                if (submit == continueBtn)
                {
                    return RedirectToAction("CreateDates", new { id = contract.Id });
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            //Repeat Model Initialization of SelectLists -> See GET: ActionMethod
            model = CreateGeneralHelper(model);
            //initialization:end

            return View(model);
        }


        //CreateGeneralHelper
        public ContractCreateGeneralViewModel CreateGeneralHelper(ContractCreateGeneralViewModel model)
        {
            Contract contract = db.Contracts.Find(model.ContractId);
            if (contract != null)
            {
                //Fill model with Data from Reload-Model or from current contract, if Reload-Model is empty
                model.ContractKindId = (model.ContractKindId != null) ? model.ContractKindId : ((contract.ContractKind != null) ? (int?)contract.ContractKind.Id : null);
                model.ContractTypeId = (model.ContractTypeId != null) ? model.ContractTypeId : ((contract.ContractType != null) ? (int?)contract.ContractType.Id : null);
                model.ContractSubTypeId = (model.ContractSubTypeId != null) ? model.ContractSubTypeId : ((contract.ContractSubType != null) ? (int?)contract.ContractSubType.Id : null);
                model.DepartmentId = (model.DepartmentId != null) ? model.DepartmentId : ((contract.Department != null) ? (int?)contract.Department.Id : null);
                model.DepartmentId = (model.SupervisorDepartmentId != null) ? model.SupervisorDepartmentId : ((contract.SupervisorDepartment != null) ? (int?)contract.SupervisorDepartment.Id : null);
                model.Remarks = (model.Remarks != null) ? model.Remarks : contract.Remarks;
                model.ExtContractNum = (model.ExtContractNum != null) ? model.ExtContractNum : contract.ExtContractNum;
                // If Contract is FrameContract
                if (contract.IsFrameContract == true)
                {
                    model.FrameOptionChosen = "FrameMain";
                }
                else
                {
                    //If Contract is Subcontract
                    if (contract.FrameContract != null)
                    {
                        model.MainFrameIdSelected = contract.FrameContract.Id;
                        model.FrameOptionChosen = "FrameSub";
                    }
                }
                //If a new DocAdress was given
                if (model.PhysicalDocAdressId == 0)
                {
                    if (contract.PhysicalDocAddress != null)
                    {
                        model.PhysicalDocAdressId = contract.PhysicalDocAddress.Id;
                        model.PDA_Adress = contract.PhysicalDocAddress.Address;
                        model.PDA_DepartmentId = contract.PhysicalDocAddress.Department.Id;
                    }
                }
                model.ContractPartnerId = (model.ContractPartnerId != 0) ? model.ContractPartnerId : ((contract.ContractPartner != null) ? (int?)contract.ContractPartner.Id : null);
            }

            var userId = User.Identity.GetUserId();
            var currentUser = manager.FindById(userId);
            //Set the types and kinds
            model.ContractKinds = new SelectList(db.ContractKinds, "Id", "Description", model.ContractKindId);
            model.ContractTypes = new SelectList(db.ContractTypes, "Id", "Description", model.ContractTypeId);
            model.ContractSubTypes = new SelectList(db.ContractSubTypes, "Id", "Description", model.ContractSubTypeId);
            model.ContractPartners = new SelectList(db.ContractPartners, "Id", "Name", model.ContractPartnerId);

            //Gets Department of inlogged User
            var currentDepartment = QueryUtility.GetDepartmentsOfUser(currentUser.UserName, db);
            //Get Client of first Department
            var currentClient = QueryUtility.GetClientOfDepartment(currentDepartment.Select(d => d.DepartmentName).FirstOrDefault(), db);
            //Get all Departments from Client
            var DepartmentsFromClient = QueryUtility.GetDepartmentsFromClient(currentClient.Select(d => d.ClientName).FirstOrDefault(), db);

            //Init and set Department of inlogged User as Default value
            //Doesn't work now with default value;
            if (DepartmentsFromClient.Any())
            {
                model.Departments = new SelectList(DepartmentsFromClient, "Id", "DepartmentName", currentDepartment.FirstOrDefault().Id);
            }
            else
            {
                model.Departments = new SelectList(new[] { "No Departments found" });
            }

            //Christoph: set up the Dropdownlist for FrameContractChoice:
            List<SelectListItem> frameContractChoice = new List<SelectListItem>();
            frameContractChoice.Add(new SelectListItem
            {
                Text = "Kein Rahmenvertrag",
                Value = "NoFrame",
                Selected = true
            });
            frameContractChoice.Add(new SelectListItem
            {
                Text = "Ist Hauptvertrag",
                Value = "FrameMain"
            });
            frameContractChoice.Add(new SelectListItem
            {
                Text = "Ist Untervertrag",
                Value = "FrameSub"
            });
            model.FrameContractChoice = frameContractChoice;
            var frameContracts = QueryUtility.GetFrameContractsOfUser(currentUser.UserName, db);
            model.MainFrameContracts = new SelectList(frameContracts, "Id", "Description", "---Select framecontract---");
            //Add Contract to View to display its information in following Forms (like Status and Name)
            model.Contract = contract;

            return model;
        }


        //*********************************************************************************************************************************
        // GET: Contract/Create/CreateDates
        public ActionResult CreateDates(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //David Create and populate the ViewModel
            var model = new ContractCreateDatesViewModel();
            //Set the Contract Id !!before initialization with Helper
            model.ContractId = (int)id;
            model = CreateDatesHelper(model);

            return View(model);
        }

        [HttpPost]
        public ActionResult CreateDates(ContractCreateDatesViewModel model, string submit)
        {
            if (ModelState.IsValid) //If all values are accepted
            {
                //load contract
                var contract = db.Contracts.Find(model.ContractId);

                //set contract from model
                contract.ContractBegin = model.ContractBegin;
                //db.Entry(contract).Reference(c => c.ContractEnd).Load();
                if (model.ContractEnd != null)
                {
                    contract.ContractEnd = model.ContractEnd;
                }
                else
                {
                    db.Entry(contract).Reference(c => c.CancellationPeriod).Load();
                    contract.CancellationPeriod = model.CancellationPeriod;
                }
                contract.CancellationDate = model.CancellationDate;
                contract.MinContractDuration = model.MinContractDuration;
                contract.AutoExtension = model.AutoExtension;

                //Set Contract Status
                contract.ContractStatus = HelperUtility.checkContractStatus(contract, db);

                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();

                //Decide which button was pressed...then redirect
                if (submit == continueBtn)
                {
                    return RedirectToAction("CreateCosts", new { id = contract.Id });
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            //Repeat Model Initialization of SelectLists -> See GET: ActionMethod
            model = CreateDatesHelper(model);
            //initialization:end

            return View(model);
        }

        //CreateDatesHelper
        public ContractCreateDatesViewModel CreateDatesHelper(ContractCreateDatesViewModel model)
        {
            Contract contract = db.Contracts.Find(model.ContractId);
            if (contract != null)
            {
                model.ContractBegin = (model.ContractBegin != null) ? model.ContractBegin : contract.ContractBegin;
                model.ContractEnd = (model.ContractEnd != null) ? model.ContractEnd : contract.ContractEnd;
                model.CancellationPeriod = (model.CancellationPeriod != null) ? model.CancellationPeriod : ((contract.CancellationPeriod != null) ? contract.CancellationPeriod : null);
                model.CancellationDate = (model.CancellationDate != null) ? model.CancellationDate : contract.CancellationDate;
                model.MinContractDuration = (model.MinContractDuration != null) ? model.MinContractDuration : contract.MinContractDuration;
                model.AutoExtension = (model.AutoExtension != null) ? model.AutoExtension : contract.AutoExtension;

            }

            //Enum does not need Dropdownlist initialization: Runtimetypes

            //Add Contract to View to display its information in following Forms (like Status and Name)
            model.Contract = contract;

            return model;
        }

        //*********************************************************************************************************************************
        // GET: Contract/Create/CreateCosts
        public ActionResult CreateCosts(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //David Create and populate the ViewModel
            var model = new ContractCreateCostsViewModel();
            //Set the Contract Id !!before initialization with Helper
            model.ContractId = (int)id;
            model = CreateCostsHelper(model);

            return View(model);
        }

        [HttpPost]
        public ActionResult CreateCosts(ContractCreateCostsViewModel model, string submit)
        {
            //Initialize the Lists, which aren't brought by HiddenFor's or Form-Data
            model.ContractCostCenter_Relations = (List<ContractCostCenter_Relation>)TempData["ContractCostCenter_Relations"];
            model.CostCenterIds = (List<int?>)TempData["CostCenterIds"];
            model.CostCenterSelectLists = (List<IEnumerable<SelectListItem>>)TempData["CostCenterSelectLists"];
            model.CostCenterPercentages = (List<double?>)TempData["CostCenterPercentages"];

            //The option to add a CostCenter on submit
            if (submit == "addCostCenter")
            {
                model = AddCostCenter(model);
                //model = CreateCostsHelper(model); not necessary, because done in Remove-Method
                return View(model);
            }
            //Option to remove the selected Option brought by model.CostCenterIndex
            else if (submit == "removeCostCenter")
            {
                model = RemoveCostCenter(model, (int)model.CostCenterIndex);
                //model = CreateCostsHelper(model); not necessary, because done in Remove-Method
                return View(model);
            }

            //The normal Validation
            if (ModelState.IsValid) //If all values are accepted
            {
                //load contract
                var contract = db.Contracts.Find(model.ContractId);

                //set contract from model
                contract.ContractValue = model.ContractValue;
                contract.AnnualValue = model.AnnualValue;
                contract.PaymentBegin = model.PaymentBegin;
                contract.PaymentInterval = model.PaymentInterval;
                contract.Tax = model.Tax;
                contract.PrePayable = model.PrePayable;
                contract.VarPayable = model.VarPayable;
                contract.Adaptable = model.Adaptable;

                //CostCenter
                contract.ContractCostCenter_Relations = new List<ContractCostCenter_Relation>();
                int i = 0;
                foreach (ContractCostCenter_Relation cCenter in model.ContractCostCenter_Relations)
                {
                    if (model.CostCenterIds[i] != null)
                    {
                        cCenter.CostCenterId = (int)model.CostCenterIds[i];
                        cCenter.Percentage = (double)model.CostCenterPercentages[i];
                        contract.ContractCostCenter_Relations.Add(cCenter);
                    }
                    i++;
                }
                //and finally CostKind
                contract.CostKind = db.CostKinds.Find(model.CostKindId);

                //Set Contract Status from HelperUtility
                contract.ContractStatus = HelperUtility.checkContractStatus(contract, db);

                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();

                //Decide which button was pressed...then redirect
                if (submit == continueBtn)
                {
                    return RedirectToAction("CreateFiles", new { id = contract.Id });
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            //Repeat Model Initialization of SelectLists -> See GET: ActionMethod
            model = CreateCostsHelper(model);
            //initialization:end

            return View(model);
        }

        //CreateCostsHelper
        public ContractCreateCostsViewModel CreateCostsHelper(ContractCreateCostsViewModel model)
        {
            //Define Contract, which is currently loaded
            Contract contract = db.Contracts.Find(model.ContractId);
            if (contract != null)
            {
                //Decide, if contract in DB or the current model have the updated Data and select this one with data in it - or, if both have data, use model.
                model.ContractValue = (model.ContractValue != null) ? model.ContractValue : contract.ContractValue;
                model.AnnualValue = (model.AnnualValue != null) ? model.AnnualValue : contract.AnnualValue;
                model.PaymentBegin = (model.PaymentBegin != null) ? model.PaymentBegin : contract.PaymentBegin;
                model.PaymentInterval = (model.PaymentInterval != null) ? model.PaymentInterval : contract.PaymentInterval;
                model.Tax = (model.Tax != null) ? model.Tax : ((contract.Tax != null) ? contract.Tax : 1.19); ;
                model.PrePayable = (model.PrePayable == true) ? true : ((contract.PrePayable != null) ? (bool)contract.PrePayable : false);
                model.VarPayable = (model.VarPayable == true) ? true : ((contract.VarPayable != null) ? (bool)contract.VarPayable : false);
                model.Adaptable = (model.Adaptable == true) ? true : ((contract.Adaptable != null) ? (bool)contract.Adaptable : false);

                //Initialize CostCenterIds
                if (model.ContractCostCenter_Relations == null)
                {
                    //Case: The model has no Relation → model Ids must set and SelectLists must set;
                    model.CostCenterIds = new List<int?>();
                    model.CostCenterPercentages = new List<double?>();
                    model.CostCenterSelectLists = new List<IEnumerable<SelectListItem>>();

                    if (contract.ContractCostCenter_Relations != null)
                    {
                        //Case if contract in db has already a Relation → The Relations can be assumed.
                        model.ContractCostCenter_Relations = contract.ContractCostCenter_Relations.ToList();
                        //For each Relation, create an Id, where you can set the selected value later.
                        int j = 0;
                        foreach (ContractCostCenter_Relation cCenter in contract.ContractCostCenter_Relations)
                        {
                            model.CostCenterIds[j] = cCenter.Id;
                            model.CostCenterPercentages[j] = cCenter.Percentage;
                            j++;
                        }
                    }
                    else
                    {
                        //Case: no Relations exists, so it is initialized with an empty one;
                        model.ContractCostCenter_Relations = new List<ContractCostCenter_Relation>();
                    }
                }
                //And finally the Kinds again
                model.CostKindId = (model.CostKindId != null) ? model.CostKindId : ((contract.CostKind != null) ? (int?)contract.CostKind.Id : null);

            } //Else case not necessary, because, if no contract exists nothing can happen

            //Enum does not need Dropdownlist initialization: Runtimetypes
            int i = 0;
            foreach (ContractCostCenter_Relation cCenter in model.ContractCostCenter_Relations)
            {

                model.CostCenterSelectLists[i] = new SelectList(db.CostCenters, "Id", "Description", model.CostCenterIds[i]);
                i++;
            }
            //Selectlist initialization
            model.CostKinds = new SelectList(db.CostKinds, "Id", "Description", model.CostKindId);

            model.Clients = new SelectList(db.Clients, "Id", "ClientName");
            //Add Contract to View to display its information in following Forms (like Status and Name)
            model.Contract = contract;
            if (model.CostCenterIndex == null)
            {
                model.CostCenterIndex = -1;
            }

            //Save in TempData, because HiddenFore doesn't support Lists or they must be saved in a foreach - loop which is very unefficient
            TempData["ContractCostCenter_Relations"] = model.ContractCostCenter_Relations;
            TempData["CostCenterIds"] = model.CostCenterIds;
            TempData["CostCenterSelectLists"] = model.CostCenterSelectLists;
            TempData["CostCenterPercentages"] = model.CostCenterPercentages;

            return model;
        }

        //Adds a CostCenter to CostCenter_Relation
        public ContractCreateCostsViewModel AddCostCenter(ContractCreateCostsViewModel model)
        {

            var cRelation = new ContractCostCenter_Relation();
            cRelation.ContractId = model.ContractId;

            //Sum all Percentages and calculate new init Value
            double allPercentages = 0;
            foreach (ContractCostCenter_Relation tempRelation in model.ContractCostCenter_Relations)
            {
                allPercentages += tempRelation.Percentage;
            }
            cRelation.Percentage = 1 - allPercentages;

            model.ContractCostCenter_Relations.Add(cRelation);
            int last = model.ContractCostCenter_Relations.Count - 1;

            var CostCenterList = new SelectList(db.CostCenters, "Id", "Description");
            model.CostCenterSelectLists.Add(CostCenterList);

            model.CostCenterIds.Add(null);
            model.CostCenterPercentages.Add(null);
            //Repeat Model Initialization of SelectLists -> See GET: ActionMethod
            model = CreateCostsHelper(model);
            //initialization:end

            return model;
        }

        //Removes a CostCenter in CostCenter_Relation by index
        public ContractCreateCostsViewModel RemoveCostCenter(ContractCreateCostsViewModel model, int index)
        {

            var relations = model.ContractCostCenter_Relations;
            int last = model.ContractCostCenter_Relations.Count - 1;
            //Set the last elemt to the deleted ones;
            model.ContractCostCenter_Relations[index] = model.ContractCostCenter_Relations[last];
            model.CostCenterSelectLists[index] = model.CostCenterSelectLists[last];
            model.CostCenterIds[index] = model.CostCenterIds[last];
            model.CostCenterPercentages[index] = model.CostCenterPercentages[last];
            //Then delet the last elements → we make sure, that no entry keeps empty
            model.ContractCostCenter_Relations.RemoveAt(last);
            model.CostCenterSelectLists.RemoveAt(last);
            model.CostCenterIds.RemoveAt(last);
            model.CostCenterPercentages.RemoveAt(last);

            //Repeat Model Initialization of SelectLists -> See GET: ActionMethod
            model = CreateCostsHelper(model);
            //initialization:end

            return model;
        }


        //*********************************************************************************************************************************
        // GET: Contract/CreateFiles
        public ActionResult CreateFiles(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            }

            var FilesViewModel = new ContractCreateFilesViewModel();
            var Contract = db.Contracts.Find(id);
            FilesViewModel.ContractId = Contract.Id;
            List<ContractFile> userFiles = (from r in db.ContractFiles where r.Contract.Id == id select r).ToList();
            FilesViewModel.FileCount = userFiles.Count;
            FilesViewModel.Files = userFiles;

            return View(FilesViewModel);
        }

        // POST: Contract/CreateFiles
        [HttpPost]
        public ActionResult CreateFiles(ContractCreateFilesViewModel FilesViewModel, string submit)
        {
            HttpPostedFileBase tmp;
            var Contract = db.Contracts.Find(FilesViewModel.ContractId);

            foreach (var file in FilesViewModel.FilesUpload)
            {
                if (file != null)
                {
                    string ContainerName = "cristo"; //hardcoded container name. 
                    tmp = file ?? Request.Files["file"];
                    string fileName = Path.GetFileName(tmp.FileName);
                    Stream imageStream = tmp.InputStream;
                    var result = utility.UploadBlob(fileName, ContainerName, imageStream);
                    if (result != null)
                    {

                        ContractFile userfile = new ContractFile();
                        userfile.FileName = Path.GetFileName(tmp.FileName);
                        userfile.FileType = Path.GetExtension(tmp.FileName);
                        userfile.FileUrl = "https://sopro16.blob.core.windows.net/cristo" + "/" + Path.GetFileName(tmp.FileName);
                        userfile.Contract = Contract;
                        db.ContractFiles.Add(userfile);
                        db.SaveChanges();
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }
            }

            db.Entry(Contract).State = EntityState.Modified;
            db.SaveChanges();

            if (submit == "Save File")
            {
                return RedirectToAction("CreateFiles", new { id = Contract.Id });
            }
            else
            {
                return RedirectToAction("Index");
            }


        }
        public ActionResult DeleteFile(int? id)
        {
            int ContractId;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            }
            ContractFile userFile = db.ContractFiles.Find(id);
            ContractId = userFile.Contract.Id;
            db.ContractFiles.Remove(userFile);
            db.SaveChanges();
            string BlobNameToDelete = userFile.FileUrl.Split('/').Last();
            utility.DeleteBlob(BlobNameToDelete, "cristo");  //hardcoded container name. 

            return RedirectToAction("CreateFiles", new { id = ContractId });
        }

        //==============================================================================
        //Json Queries for Javascript Dropdown
        public ActionResult GetJsonCoordinatorsFromClient(string client)
        {
            var data = new SelectList(QueryUtility.GetCoordinatorsFromClient(client, db), "Id", "UserName").ToList();
            return Json(data);
        }

        public ActionResult GetJsonDispatchersFromDepartment(string department)
        {
            var data = new SelectList(QueryUtility.GetDispatchersFromDepartment(department, db), "Id", "UserName").ToList();
            return Json(data);
        }

        public ActionResult GetJsonDepartmentsFromClient(string client)
        {
            var data = new SelectList(QueryUtility.GetDepartmentsFromClient(client, db), "Id", "DepartmentName").ToList();
            return Json(data);
        }

        public ActionResult GetJsonUsersFromDepartment(string department)
        {
            var data = new SelectList(QueryUtility.GetUsersFromDepartment(department, db), "Id", "UserName").ToList();
            return Json(data);
        }

        public ActionResult GetJsonContractSubTypesFromContractTypes(string type)
        {
            var data = new SelectList(QueryUtility.GetContractSubTypesFromContractTypes(type, db), "Id", "Description").ToList();
            return Json(data);
        }

        public ActionResult GetJsonCostCentersFromClient(string client)
        {
            var data = new SelectList(QueryUtility.GetCostCentersFromClient(client, db), "Id", "Description").ToList();
            return Json(data);
        }

        //-------------------- Moses ------------------
        // GET: Contract/AdvancedSearch
        public ActionResult AdvancedSearch()
        {
            var AdvancedSearchViewModel = new ContractAdvancedSearchViewModel();
            if (db.ContractStatuses != null)
            {
                AdvancedSearchViewModel.ContractStatuses = new SelectList(db.ContractStatuses, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.ContractStatuses = new SelectList(new[] { "No Status" });
            }


            if (db.ContractTypes != null)
            {
                AdvancedSearchViewModel.ContractTypes = new SelectList(db.ContractTypes, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.ContractTypes = new SelectList(new[] { "No Type" });
            }


            if (db.ContractSubTypes != null)
            {
                AdvancedSearchViewModel.ContractSubTypes = new SelectList(db.ContractSubTypes, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.ContractSubTypes = new SelectList(new[] { "No ContractSubType" });
            }


            if (db.ContractKinds != null)
            {
                AdvancedSearchViewModel.ContractKinds = new SelectList(db.ContractKinds, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.ContractKinds = new SelectList(new[] { "No Kind" });
            }
            if (db.Departments != null)
            {
                AdvancedSearchViewModel.Departments = new SelectList(db.Departments, "Id", "DepartmentName");
            }
            else
            {
                AdvancedSearchViewModel.Departments = new SelectList(new[] { "No Department" });
            }
            if (db.CostCenters != null)
            {
                AdvancedSearchViewModel.CostCenters = new SelectList(db.CostCenters, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.CostCenters = new SelectList(new[] { "No CostCenter" });
            }
            if (db.CostKinds != null)
            {
                AdvancedSearchViewModel.CostKinds = new SelectList(db.CostKinds, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.CostKinds = new SelectList(new[] { "No CostKind" });
            }



            return View(AdvancedSearchViewModel);
        }

        // POST: Contract/AdvancedSearch
        [HttpPost]
        public ActionResult AdvancedSearch(ContractAdvancedSearchViewModel AdvancedSearchViewModel)
        {
            if (db.ContractStatuses != null)
            {
                AdvancedSearchViewModel.ContractStatuses = new SelectList(db.ContractStatuses, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.ContractStatuses = new SelectList(new[] { "No Status" });
            }


            if (db.ContractTypes != null)
            {
                AdvancedSearchViewModel.ContractTypes = new SelectList(db.ContractTypes, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.ContractTypes = new SelectList(new[] { "No Type" });
            }


            if (db.ContractSubTypes != null)
            {
                AdvancedSearchViewModel.ContractSubTypes = new SelectList(db.ContractSubTypes, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.ContractSubTypes = new SelectList(new[] { "No ContractSubType" });
            }


            if (db.ContractKinds != null)
            {
                AdvancedSearchViewModel.ContractKinds = new SelectList(db.ContractKinds, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.ContractKinds = new SelectList(new[] { "No Kind" });
            }
            if (db.Departments != null)
            {
                AdvancedSearchViewModel.Departments = new SelectList(db.Departments, "Id", "DepartmentName");
            }
            else
            {
                AdvancedSearchViewModel.Departments = new SelectList(new[] { "No Department" });
            }
            if (db.CostCenters != null)
            {
                AdvancedSearchViewModel.CostCenters = new SelectList(db.CostCenters, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.CostCenters = new SelectList(new[] { "No CostCenter" });
            }
            if (db.CostKinds != null)
            {
                AdvancedSearchViewModel.CostKinds = new SelectList(db.CostKinds, "Id", "Description");
            }
            else
            {
                AdvancedSearchViewModel.CostKinds = new SelectList(new[] { "No CostKind" });
            }



            var search = new ContractSearchLogic();
            string CmdQueryText = search.GenerateQuery(AdvancedSearchViewModel);
            search.GetContracts(CmdQueryText, AdvancedSearchViewModel);

            return View(AdvancedSearchViewModel);
        }

    }
}
