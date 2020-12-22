using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Localization;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.Security;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.DB.Helpers;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using XafSolution.Module.BusinessObjects;

namespace BlazorClientSideApplication {
    public static class XpoHelper {
        static SecuredObjectSpaceProvider ObjectSpaceProvider;
        static AuthenticationStandard Authentication;
        public static cusomSecurityStrategyComplex Security;

        public static async Task InitXpo(string connectionString, string login, string password) {
            Tracing.UseConfigurationManager = false;
            Tracing.Initialize(3);
            RegisterEntities();
            InitSecurity();
            XpoDefault.RegisterBonusProviders();
            DataStoreBase.RegisterDataStoreProvider(WebApiDataStoreClient.XpoProviderTypeString, CreateWebApiDataStoreFromString);
            ObjectSpaceProvider = new SecuredObjectSpaceProvider(Security, connectionString, null, false);
            await UpdateDataBase();
            await LogIn(login, password);
            XpoDefault.Session = null;
        }

        static async Task UpdateDataBase() {
            var space = ObjectSpaceProvider.CreateUpdatingObjectSpace(true);
            Updater updater = new Updater(space);
            await updater.UpdateDatabase();
        }

        public static UnitOfWork CreateUnitOfWork() {
            var space = (XPObjectSpace)ObjectSpaceProvider.CreateObjectSpace();
            return (UnitOfWork)space.Session;
        }
        static async Task LogIn(string login, string password) {
            Authentication.SetLogonParameters(new AuthenticationStandardLogonParameters(login, password));
            IObjectSpace loginObjectSpace = ObjectSpaceProvider.CreateObjectSpace();
            await Security.LogonAsync(loginObjectSpace);
        }

        static void InitSecurity() {
            Authentication = new CustomStandart();
            Security =  new cusomSecurityStrategyComplex(typeof(PermissionPolicyUser), typeof(PermissionPolicyRole), Authentication);
            Security.RegisterXPOAdapterProviders();
        }
        private static void RegisterEntities() {
            XpoTypesInfoHelper.GetXpoTypeInfoSource();
            XafTypesInfo.Instance.RegisterEntity(typeof(Employee));
            XafTypesInfo.Instance.RegisterEntity(typeof(PermissionPolicyUser));
            XafTypesInfo.Instance.RegisterEntity(typeof(PermissionPolicyRole));
        }

        static IDataStore CreateWebApiDataStoreFromString(string connectionString, AutoCreateOption autoCreateOption, out IDisposable[] objectsToDisposeOnDisconnect) {
            ConnectionStringParser parser = new ConnectionStringParser(connectionString);
            if(!parser.PartExists("uri"))
                throw new ArgumentException("Connection string does not contain the 'uri' part.");
            string uri = parser.GetPartByName("uri");
#if DEBUG
            HttpClient client = new HttpClient();
#else
            HttpClient client = new HttpClient();
#endif
            client.BaseAddress = new Uri(uri);
            objectsToDisposeOnDisconnect = new IDisposable[] { client };
            return new WebApiDataStoreClient(client, autoCreateOption);
        }
        static HttpClientHandler GetInsecureHandler() {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            return handler;
        }

        class CustomStandart : AuthenticationStandard {

            public  async Task<object> AuthenticateAsync(IObjectSpace objectSpace) {
                Guard.ArgumentNotNull(LogonParameters, "logonParameters");
                if(string.IsNullOrEmpty(((AuthenticationStandardLogonParameters)LogonParameters).UserName)) {
                    throw new UserFriendlySecurityException(SecurityExceptionId.UserNameIsEmpty);
                }
                IAuthenticationStandardUser user = (IAuthenticationStandardUser) await((IObjectSpaceAsync)objectSpace).FindObjectAsync(UserType, new BinaryOperator("UserName", ((AuthenticationStandardLogonParameters)LogonParameters).UserName), false);

                if(user == null || !user.ComparePassword(((AuthenticationStandardLogonParameters)LogonParameters).Password)) {
                    throw new AuthenticationException(((AuthenticationStandardLogonParameters)LogonParameters).UserName, SecurityExceptionLocalizer.GetExceptionMessage(SecurityExceptionId.RetypeTheInformation));
                }
                return user;
            }
        }
        public class cusomSecurityStrategyComplex : SecurityStrategyComplex {

            public cusomSecurityStrategyComplex(Type type, Type type1, AuthenticationBase Authentication):base(type,type1,Authentication) { 
            }
            public async Task LogonAsync(IObjectSpace objectSpace) { //TODO: ask AVM for a special method.
                if(Authentication == null) {
                    throw new InvalidOperationException("authentication is null");
                }
                //isAuthenticated = false;

                IObjectSpace directObjectSpace = AdapterFacade.GetRealObjectSpace(objectSpace);
                object currentUser = await ((CustomStandart)Authentication).AuthenticateAsync(directObjectSpace);
                if(currentUser == null) {
                    throw new InvalidOperationException("The Authentication.Authenticate method returned 'null'. It should return an object or raise an exception.");
                }
                if(!userType.IsAssignableFrom(currentUser.GetType())) {
                    throw new InvalidCastException(SystemExceptionLocalizer.GetExceptionMessage(ExceptionId.UnableToCast,
                        currentUser.GetType(),
                        userType));
                }
                Logon(currentUser);
                //isAuthenticated = true;
                logonObjectSpace = objectSpace;
                //if(LoggingOn != null) {
                 //   LoggingOnEventArgs loggingOnArgs = new LoggingOnEventArgs(logonObjectSpace, userType, UserId);
                   // LoggingOn?.Invoke(this, loggingOnArgs);
                //}
            }

        }
    }
}