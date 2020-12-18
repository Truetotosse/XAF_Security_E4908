using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using XafSolution.Module.BusinessObjects;

namespace BlazorClientSideApplication {
    public class Updater {
        private const string AdministratorUserName = "Admin";
        private const string AdministratorRoleName = "Administrators";
        private const string DefaultUserName = "User";
        private const string DefaultUserRoleName = "Users";
        private XPObjectSpace ObjectSpace { get; }
        private UnitOfWork uow { get; }
        public Updater(IObjectSpace objectSpace) { ObjectSpace = objectSpace as XPObjectSpace; }

        public async Task UpdateDatabase() {
            await CreateUser();
            await CreateAdmin();
            CreateEmployees();
        }
        private async Task CreateUser() {
            PermissionPolicyUser defaultUser = await ObjectSpace.FindObjectAsync<PermissionPolicyUser>("[UserName] == 'User'",false);
            if(defaultUser == null) {
                defaultUser = ObjectSpace.CreateObject<PermissionPolicyUser>();
                defaultUser.UserName = DefaultUserName;
                defaultUser.SetPassword("");
                defaultUser.Roles.Add(await GetUserRole());
                await ObjectSpace.CommitChangesAsync();
            }
        }
        private async Task CreateAdmin() {
            PermissionPolicyUser adminUser = await ObjectSpace.FindObjectAsync<PermissionPolicyUser>("[UserName] == 'Admin'",false);
            if(adminUser == null) {
                adminUser = ObjectSpace.CreateObject<PermissionPolicyUser>();
                adminUser.UserName = AdministratorUserName;
                adminUser.SetPassword("");
                adminUser.Roles.Add(await GetAdminRole());
                await ObjectSpace.CommitChangesAsync();
            }
        }
        private async Task<PermissionPolicyRole> GetAdminRole() {
            PermissionPolicyRole adminRole = await ObjectSpace.FindObjectAsync<PermissionPolicyRole>(String.Format("[Name]='{0}'",AdministratorRoleName),false);
            if(adminRole == null) {
                adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                adminRole.Name = AdministratorRoleName;
                adminRole.IsAdministrative = true;
            }
            return adminRole;
        }
        private async Task<PermissionPolicyRole> GetUserRole() {
            PermissionPolicyRole userRole = await ObjectSpace.FindObjectAsync<PermissionPolicyRole>(String.Format("[Name]='{0}'", DefaultUserRoleName),false);
            if(userRole == null) {
                userRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                userRole.Name = DefaultUserRoleName;
                // Allow users to read departments only if their title contains 'Development'. 
                const string protectedDepartment = "Development";
                CriteriaOperator departmentCriteria = new FunctionOperator(FunctionOperatorType.Contains,
                    new OperandProperty(nameof(Department.Title)), new OperandValue(protectedDepartment)
                );
                userRole.AddObjectPermission<Department>(SecurityOperations.Read, (!departmentCriteria).ToString() /*"!Contains(Title, 'Development')"*/, SecurityPermissionState.Deny);
                // Allow users to read and modify employee records and their fields by criteria.
                userRole.AddTypePermissionsRecursively<Employee>(SecurityOperations.Read, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<Employee>(SecurityOperations.Write, SecurityPermissionState.Allow);
                CriteriaOperator employeeCriteria = new FunctionOperator(FunctionOperatorType.Contains,
                    new OperandProperty(nameof(Employee.Department) + "." + nameof(Department.Title)), new OperandValue(protectedDepartment)
                );
                userRole.AddObjectPermission<Employee>(SecurityOperations.Delete, employeeCriteria.ToString()/*"Contains(Department.Title, 'Development')"*/, SecurityPermissionState.Allow);
                userRole.AddMemberPermission<Employee>(SecurityOperations.Write, nameof(Employee.LastName), (!employeeCriteria).ToString() /*"!Contains(Department.Title, 'Development')"*/, SecurityPermissionState.Deny);
                // For more information on criteria language syntax (both string and strongly-typed formats), see https://docs.devexpress.com/CoreLibraries/4928/.
            }
            return userRole;
        }
        private async  void CreateEmployees() {
            DataTable employeesTable = GetEmployeesDataTable();
            foreach(DataRow employeeRow in employeesTable.Rows) {
                string email = Convert.ToString(employeeRow["EmailAddress"]);
                Employee employee = await ObjectSpace.FindObjectAsync<Employee>(String.Format("[Email]='{0}'",email),false);
                if(employee == null) {
                    employee = ObjectSpace.CreateObject<Employee>();
                    employee.Email = email;
                    employee.FirstName = Convert.ToString(employeeRow["FirstName"]);
                    employee.LastName = Convert.ToString(employeeRow["LastName"]);
                    employee.Birthday = Convert.ToDateTime(employeeRow["BirthDate"]);

                    string departmentTitle = Convert.ToString(employeeRow["GroupName"]);
                    Department department = await ObjectSpace.FindObjectAsync<Department>(String.Format("[Title]='{0}'", departmentTitle), false);
                    if(department == null) {
                        department = ObjectSpace.CreateObject<Department>();
                        department.Title = departmentTitle;
                        Random rnd = new Random();
                        department.Office = $"{rnd.Next(1, 7)}0{rnd.Next(9)}";
                    }
                    employee.Department = department;
                }
            }
            await ObjectSpace.CommitChangesAsync();
        }
        private DataTable GetEmployeesDataTable() {
            string shortName = "Employees.xml";
            string embeddedResourceName = Array.Find<string>(this.GetType().Assembly.GetManifestResourceNames(), (s) => { return s.Contains(shortName); });
            Stream stream = this.GetType().Assembly.GetManifestResourceStream(embeddedResourceName);
            if(stream == null) {
                throw new Exception(string.Format("Cannot read employees data from the {0} file!", shortName));
            }
            stream.Position = 0;
            DataSet ds = new DataSet();
            ds.ReadXml(stream);
            return ds.Tables["Employee"];
        }
    }
}
