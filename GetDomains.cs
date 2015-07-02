using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections.Specialized;

using System.Runtime.InteropServices;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.SOESupport;
using ESRI.ArcGIS.DataSourcesGDB;


//TODO: sign the project (project properties > signing tab > sign the assembly)
//      this is strongly suggested if the dll will be registered using regasm.exe <your>.dll /codebase


namespace GetDomains
{
    [ComVisible(true)]
    [Guid("cbb09029-e5fd-4bd4-8cf6-f862c2af64fa")]
    [ClassInterface(ClassInterfaceType.None)]
    [ServerObjectExtension("MapServer",
        AllCapabilities = "ReportDomains,MakeDomainOptions",
        DefaultCapabilities = "ReportDomains,MakeDomainOptions",
        Description = "Get Domain values for a featureclass",
        DisplayName = "GetDomains",
        Properties = "Server=Venus;Instance=5264;Database=*****;Version=SDE.DEFAULT;User=***;Password=***;",
        //HasManagerPropertiesConfigurationPane = true,
        SupportsREST = true,
        SupportsSOAP = false)]
    public class GetDomains : IServerObjectExtension, IObjectConstruct, IRESTRequestHandler
    {
        private string soe_name;

        private IPropertySet configProps;
        private IServerObjectHelper serverObjectHelper;
        private ServerLogger logger;
        private IRESTRequestHandler reqHandler;

        string strServer, strInstance, strDatabase, strVersion, strUser, strPasswd;

        public GetDomains()
        {
            soe_name = this.GetType().Name;
            logger = new ServerLogger();
            reqHandler = new SoeRestImpl(soe_name, CreateRestSchema()) as IRESTRequestHandler;
        }

        #region IServerObjectExtension Members

        public void Init(IServerObjectHelper pSOH)
        {
            //System.Diagnostics.Debugger.Launch();
            serverObjectHelper = pSOH;

        }

        public void Shutdown()
        {
        }

        #endregion

        #region IObjectConstruct Members

        public void Construct(IPropertySet props)
        {
            configProps = props;

            strServer = props.GetProperty("Server").ToString();
            strInstance = props.GetProperty("Instance").ToString();
            strDatabase = props.GetProperty("Database").ToString();
            strVersion = props.GetProperty("Version").ToString();
            strUser = props.GetProperty("User").ToString();
            strPasswd = props.GetProperty("Password").ToString();
        }

        #endregion

        #region IRESTRequestHandler Members

        public string GetSchema()
        {
            return reqHandler.GetSchema();
        }

        public byte[] HandleRESTRequest(string Capabilities, string resourceName, string operationName, string operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return reqHandler.HandleRESTRequest(Capabilities, resourceName, operationName, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        #endregion

        private RestResource CreateRestSchema()
        {
            RestResource rootRes = new RestResource(soe_name, false, RootResHandler);

            RestOperation ReportDomainsOper = new RestOperation("ReportDomains",
                                                      new string[] { "SDEName" },
                                                      new string[] { "json" },
                                                      ReportDomainsHandler);

            rootRes.operations.Add(ReportDomainsOper);

            RestOperation MakeDomainOptions = new RestOperation("MakeDomainOptions",
                                                 new string[] { "SDEName", "fieldname" },
                                                 new string[] { "json" },
                                                 MakeDomainOptionsHandler);

            rootRes.operations.Add(MakeDomainOptions);

            return rootRes;
        }

        private byte[] RootResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = null;

            JsonObject result = new JsonObject();
            //result.AddString("hello", "world");

            return Encoding.UTF8.GetBytes(result.ToJson());
        }

        private byte[] ReportDomainsHandler(NameValueCollection boundVariables,
                                                  JsonObject operationInput,
                                                      string outputFormat,
                                                      string requestProperties,
                                                  out string responseProperties)
        {
            responseProperties = null;

            string strSDELayerName;
            bool found = operationInput.TryGetString("SDEName", out strSDELayerName);
            if (!found || string.IsNullOrEmpty(strSDELayerName))
                throw new ArgumentNullException("SDEName");



            IStandaloneTable pStandAloneTable = GetLayerfromSDE(strSDELayerName);
            IField pField;

            Dictionary<string, Dictionary<string, string>> dicResults = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, string> dicDomainValues;

            for (int i = 0; i <= pStandAloneTable.Table.Fields.FieldCount - 1; i++)
            {
                pField = pStandAloneTable.Table.Fields.get_Field(i);

                IDomain pDomain;
                pDomain = pField.Domain;

                if (pDomain != null)
                {
                    //PrintDomainValues(pField.Name, pDomain.DomainID);

                    dicDomainValues = new Dictionary<string, string>();


                    ICodedValueDomain pCodedValueDomain;
                    pCodedValueDomain = pDomain as ICodedValueDomain;
                    if (pCodedValueDomain != null)
                    {
                        //Console.WriteLine(pField.Name + ": " + pDomain.DomainID.ToString());
                        //Console.WriteLine("------------------------------------------");
                        for (int k = 0; k <= pCodedValueDomain.CodeCount - 1; k++)
                        {
                            string name = pCodedValueDomain.get_Value(k).ToString();
                            string value = pCodedValueDomain.get_Name(k).ToString();
                            dicDomainValues.Add(name, value);
                            //Console.WriteLine(name + ", " + value);
                        }
                        //Console.WriteLine("------------------------------------------");
                        //Console.WriteLine(Environment.NewLine);

                        dicResults.Add(pField.Name, dicDomainValues);
                    }


                }




            }










            JsonObject result = new JsonObject();
            result.AddObject("Domains", dicResults);
            //result.AddString("SDEName", strSDELayerName);
            

            return Encoding.UTF8.GetBytes(result.ToJson());
        }





        private byte[] MakeDomainOptionsHandler(NameValueCollection boundVariables,
                                               JsonObject operationInput,
                                                   string outputFormat,
                                                   string requestProperties,
                                               out string responseProperties)
        {
            responseProperties = null;

            string strSDELayerName;
            bool found = operationInput.TryGetString("SDEName", out strSDELayerName);
            if (!found || string.IsNullOrEmpty(strSDELayerName))
                throw new ArgumentNullException("SDEName");
            
            string strFieldName;
            found = operationInput.TryGetString("fieldname", out strFieldName);
            if (!found || string.IsNullOrEmpty(strSDELayerName))
                throw new ArgumentNullException("fieldname");



            IStandaloneTable pStandAloneTable = GetLayerfromSDE(strSDELayerName);
            IField pField;

          
            Dictionary<string, string> dicDomainValues;

            option opt = new option();
            opt.Result = "OK";
            List<Dictionary<string, string>> lstDics = new List<Dictionary<string, string>>();


            int iFieldIndex = pStandAloneTable.Table.Fields.FindField(strFieldName);
            if (iFieldIndex != -1)
            {
                pField = pStandAloneTable.Table.Fields.get_Field(iFieldIndex);

                IDomain pDomain;
                pDomain = pField.Domain;

                if (pDomain != null)
                {
                    
                    ICodedValueDomain pCodedValueDomain;
                    pCodedValueDomain = pDomain as ICodedValueDomain;
                    if (pCodedValueDomain != null)
                    {
                       
                        for (int k = 0; k <= pCodedValueDomain.CodeCount - 1; k++)
                        {
                            dicDomainValues = new Dictionary<string, string>();
                            string name = pCodedValueDomain.get_Value(k).ToString();
                            string value = pCodedValueDomain.get_Name(k).ToString();
                            dicDomainValues.Add("DisplayText", value);
                            dicDomainValues.Add("Value", name);
                            lstDics.Add(dicDomainValues);
                        }
              
                    }


                }



            }
            else
            {
                opt.Result = "Field not found";
            }



            JsonObject result = new JsonObject();
            result.AddString("Result", opt.Result);
            result.AddObject("Options", lstDics);



            return Encoding.UTF8.GetBytes(result.ToJson());
        }


        private IStandaloneTable GetLayerfromSDE(string FClassName)
        {
            string strSDEConnString = String.Format("server={0};instance={1};user={2};password={3};version={4}",strServer,strInstance,strUser,strPasswd,strVersion);

            IWorkspaceFactory2 pWorkFact = new SdeWorkspaceFactoryClass();
            IFeatureWorkspace pFWorkspace = pWorkFact.OpenFromString(strSDEConnString, 0) as IFeatureWorkspace;
            ITable ptable = pFWorkspace.OpenTable(FClassName);
            IStandaloneTable pStandAloneTable = new StandaloneTableClass();
            pStandAloneTable.Table = ptable;
            return pStandAloneTable;


        }


    }

    public class option
    {

        public string Result { get; set; }
        public List<Dictionary<string, string>> Options { get; set; }
    }
}
