using Laundry_Management.Laundry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Laundry_Management
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void btnAdd_Type_Service_Click(object sender, EventArgs e)
        {
            var addTypeServiceForm = new Add_Type__Service();
            addTypeServiceForm.ShowDialog();
        }

        private void btnService_Click(object sender, EventArgs e)
        {
            var ServiceForm = new Service();
            ServiceForm.ShowDialog();
        }

        private void btnCustomer_Click(object sender, EventArgs e)
        {
            var CustomerForm = new Customer();
            CustomerForm.ShowDialog();
        }

        private void btnFind_Service_Click(object sender, EventArgs e)
        {
            var findServiceForm = new Find_Service();
            findServiceForm.ShowDialog();
        }

        private void Check_List_Click(object sender, EventArgs e)
        {
            var Check_ListForm = new Check_List();
            Check_ListForm.ShowDialog();
        }

        private void Pickup_List_Click(object sender, EventArgs e)
        {
            var Pickup_ListForm = new Pickup_List();
            Pickup_ListForm.ShowDialog();
        }

        private void btnSetting_Click(object sender, EventArgs e)
        {
            var Setting_IdForm = new Setting_Id();
            Setting_IdForm.ShowDialog();
        }

        private void Report_Click(object sender, EventArgs e)
        {
            var ReportForm = new Report();
            ReportForm.ShowDialog();
        }

        private void Modify_Service_Click(object sender, EventArgs e)
        {
            var Modify_Service_ItemForm = new Modify_Service_Item();
            Modify_Service_ItemForm.ShowDialog();
        }

        private void Vat_Report_Click(object sender, EventArgs e)
        {
            var Vat_ReportForm = new Vat_Report();
            Vat_ReportForm.ShowDialog();
        }

        private void Service_Report_Click(object sender, EventArgs e)
        {
            var Service_ReportForm = new Service_Report();
            Service_ReportForm.ShowDialog();
        }

        private void OverallReport_Click(object sender, EventArgs e)
        {
            var OverallReportForm = new OverallReport();
            OverallReportForm.ShowDialog();
        }
    }
}
