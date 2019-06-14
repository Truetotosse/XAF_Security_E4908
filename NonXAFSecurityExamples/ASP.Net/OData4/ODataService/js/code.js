﻿//$(function () {
//	$("#grid").dxDataGrid({
//		dataSource: new DevExpress.data.ODataStore({
//			url: "Employees",
//			key: "OID",
//			onLoaded: function () {
//				 Event handling commands go here
//			}
//		})
//	});
//});

$(function () {
	$("#userName").dxTextBox({
		name: "userName",
		placeholder: "User name"
	}).dxValidator({
		validationRules: [
			{ type: "required" }
		]
	});

	$("#password").dxTextBox({
		name: "Password",
		placeholder: "Password",
		mode: "password"
	});

	$("#validateAndSubmit").dxButton({
		text: "Submit",
		type: "success",
		useSubmitBehavior: true
	});
});


//$(function () {
//	$("#buttonContainer").dxButton({
//		type: "default", // or "normal" | "back" | "danger" | "success"
//		text: "Login",
//		onClick: function (e) {
//			DevExpress.ui.notify("The " + " button was clicked");
//			var x = $("usernameBox").value;
//		}
//	});
//});


//$(function () {
//	$("#usernameBox").dxTextBox({
//		placeholder: "User name"
//	});
//});

//$(function () {
//	$("#passwordBox").dxTextBox({
//		placeholder: "Password"
//	});
//});





$(function () {
	$("#grid").dxDataGrid({
		height: 800,
		remoteOperations: { /*paging: true, filtering: true, sorting: true, grouping: true, summary: true, groupPaging: true*/ },
		dataSource: new DevExpress.data.ODataStore({
			url: "Employees",
			version: 4
		}),
		//editing: {
		//	mode: "form",
		//	form: {
		//		colCount: 4
		//	},
		//	allowUpdating: true,
		//	allowAdding: true,
		//	allowDeleting: true
		//},
		//onInitNewRow: function (e) {
		//	e.data = {
		//		OrderDate: new Date()
		//	};
		//},
		columnAutoWidth: true,
		filterRow: { visible: true },
		groupPanel: { visible: true },
		grouping: { autoExpandAll: false },
		pager: {
			showInfo: true
		},
		//masterDetail: {
		//	enabled: true,
		//	template: masterDetailTemplate
		//},
		columns: [
			{
				caption: "Employee",
				calculateDisplayValue: "FullName",
				dataField: "FullName"
			},
			{
				caption: "Department",
				calculateDisplayValue: "Department",
				dataField: "Department"
			}
			//{
			//	caption: "Employee",
			//	calculateDisplayValue: "EmployeeName",
			//	dataField: "EmployeeID",
			//	lookup: {
			//		valueExpr: "EmployeeID",
			//		displayExpr: "FullName",
			//		dataSource: {
			//			paginate: true,
			//			store: DevExpress.data.AspNet.createStore({
			//				key: "EmployeeID",
			//				loadUrl: "Employees/Get"
			//			})
			//		}
			//	}
			//},
			//{ dataField: "OrderDate", dataType: "date" },
			//{ dataField: "RequiredDate", dataType: "date" },
			//{ dataField: "ShippedDate", dataType: "date" },
			//{
			//	dataField: "ShipVia",
			//	calculateDisplayValue: "ShipViaName",
			//	lookup: {
			//		valueExpr: "ShipperID",
			//		displayExpr: "CompanyName",
			//		dataSource: {
			//			paginate: true,
			//			store: DevExpress.data.AspNet.createStore({
			//				key: "ShipperID",
			//				loadUrl: "Shippers/Get"
			//			})
			//		}
			//	}
			//},
			//"Freight",
			//"ShipName",
			//"ShipAddress",
			//"ShipCity",
			//"ShipCountry"
		]
	});

	$("#textBoxContainer").dxTextBox({
		placeholder: "Type a text here..."
	});

	function masterDetailTemplate(container, options) {
		$("<div>").addClass("grid").appendTo(container).dxDataGrid({
			remoteOperations: true,
			dataSource: {
				filter: ["OrderID", "=", options.key],
				store: DevExpress.data.AspNet.createStore({
					key: ["OrderID", "ProductID"],
					loadUrl: "OrderDetails/Get",
					insertUrl: "OrderDetails/Post",
					updateUrl: "OrderDetails/Put",
					deleteUrl: "OrderDetails/Delete",
				})
			},
			showBorders: true,
			editing: {
				allowUpdating: true,
				allowAdding: true,
				allowDeleting: true
			},
			onInitNewRow: function (e) {
				e.data = {
					OrderID: options.key,
					Quantity: 1,
					Discount: 0
				}
			},
			onEditorPreparing: function (e) {
				if (e.dataField === "ProductID") {
					var dataGrid = e.component;
					var valueChanged = e.editorOptions.onValueChanged;
					e.editorOptions.onValueChanged = function (args) {
						valueChanged.apply(this, arguments);

						var product = args.component.getDataSource().items().filter(function (data) { return data.ProductID === args.value })[0];

						if (product) {
							dataGrid.cellValue(e.row.rowIndex, "UnitPrice", product.UnitPrice);
						}
					}
				}
			},
			summary: {
				totalItems: [
					{ column: "Total", summaryType: "sum", displayFormat: "Total: {0}", valueFormat: { type: "currency", precision: 2 } }
				]
			},
			columns: [
				{
					dataField: "ProductID",
					caption: "Product",
					calculateDisplayValue: "ProductName",
					lookup: {
						valueExpr: "ProductID",
						displayExpr: "ProductName",
						dataSource: {
							paginate: true,
							store: DevExpress.data.AspNet.createStore({
								key: "ProductID",
								loadUrl: "Products/Get"
							})
						}
					}
				},
				{ dataField: "UnitPrice", format: { type: "currency", precision: 2 }, allowEditing: false },
				"Quantity",
				"Discount",
				{ dataField: "Total", format: { type: "currency", precision: 2 }, allowEditing: false, calculateCellValue: function (data) { return data.UnitPrice ? data.UnitPrice * data.Quantity * (1 - data.Discount) : null } }
			]
		})
	}
});