using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Laundry_Management.Laundry
{
    public partial class Service_Report : Form
    {
        private DataTable reportData;
        private int _currentPage = 0;
        private DataTable pivotData;

        public Service_Report()
        {
            InitializeComponent();

            // Initialize date pickers
            InitializeDatePickers();

            // Register event handlers
            this.Load += Service_Report_Load;
            dtpCreateMonthFirst.ValueChanged += DateFilter_ValueChanged;
            dtpCreateYearFirst.ValueChanged += DateFilter_ValueChanged;
            dtpCreateMonthLast.ValueChanged += DateFilter_ValueChanged;
            dtpCreateYearLast.ValueChanged += DateFilter_ValueChanged;

            // Hide original date pickers that we're not using
            dtpCreateMonthFirst.Visible = true;
            dtpCreateMonthLast.Visible = true;
        }

        private void InitializeDatePickers()
        {
            // Set up the month pickers
            dtpCreateMonthFirst.Format = DateTimePickerFormat.Custom;
            dtpCreateMonthFirst.CustomFormat = "MMMM";
            dtpCreateMonthFirst.ShowUpDown = true;
            dtpCreateMonthFirst.Value = DateTime.Now.AddMonths(-1); // Previous month

            dtpCreateMonthLast.Format = DateTimePickerFormat.Custom;
            dtpCreateMonthLast.CustomFormat = "MMMM";
            dtpCreateMonthLast.ShowUpDown = true;
            dtpCreateMonthLast.Value = DateTime.Now;

            // Set up the year pickers
            dtpCreateYearFirst.Format = DateTimePickerFormat.Custom;
            dtpCreateYearFirst.CustomFormat = "yyyy";
            dtpCreateYearFirst.ShowUpDown = true;
            dtpCreateYearFirst.Value = DateTime.Now;

            dtpCreateYearLast.Format = DateTimePickerFormat.Custom;
            dtpCreateYearLast.CustomFormat = "yyyy";
            dtpCreateYearLast.ShowUpDown = true;
            dtpCreateYearLast.Value = DateTime.Now;
        }

        private void InitializeDataGridView()
        {
            // Configure DataGridView properties for Thai language
            dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReport.AllowUserToAddRows = false;
            dgvReport.AllowUserToDeleteRows = false;
            dgvReport.ReadOnly = true;
            dgvReport.DefaultCellStyle.Font = new Font("Angsana New", 16);
            dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Angsana New", 16, FontStyle.Bold);
            dgvReport.RowTemplate.Height = 30;
            dgvReport.EnableHeadersVisualStyles = false;

            // Custom column widths for hierarchical display
            dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // Disable cell selection to keep the hierarchical display consistent
            dgvReport.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Add event handler for cell painting to show borders correctly
            dgvReport.CellPainting += DgvReport_CellPainting;
        }
        private void DgvReport_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // Skip header cells
            if (e.RowIndex < 0) return;

            // Draw borders for all cells to ensure consistency
            if (e.ColumnIndex >= 0)
            {
                // Paint the background
                e.PaintBackground(e.CellBounds, true);

                // Draw cell borders
                using (Pen p = new Pen(Color.Gray))
                {
                    e.Graphics.DrawRectangle(p, e.CellBounds.X, e.CellBounds.Y,
                        e.CellBounds.Width - 1, e.CellBounds.Height - 1);
                }

                // For numeric cells with content, align content to right
                if (e.ColumnIndex > 0 && e.Value != null && e.Value != DBNull.Value && !string.IsNullOrEmpty(e.Value.ToString()))
                {
                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Far;
                    format.LineAlignment = StringAlignment.Center;

                    // Leave a small padding on the right
                    Rectangle rect = new Rectangle(
                        e.CellBounds.X,
                        e.CellBounds.Y,
                        e.CellBounds.Width - 5,
                        e.CellBounds.Height);

                    e.Graphics.DrawString(e.Value.ToString(), e.CellStyle.Font,
                        new SolidBrush(e.CellStyle.ForeColor), rect, format);

                    e.Handled = true;
                }
                else if (e.Value == null || e.Value == DBNull.Value || string.IsNullOrEmpty(e.Value.ToString()))
                {
                    // For empty cells, just draw the border
                    e.Handled = true;
                }
            }
        }
        private void DateFilter_ValueChanged(object sender, EventArgs e)
        {
            // Validate date range
            if (!ValidateDateRange())
                return;

            // Load the report data with the new date range
            LoadReportData();
        }

        private bool ValidateDateRange()
        {
            DateTime startDate = new DateTime(
                dtpCreateYearFirst.Value.Year,
                dtpCreateMonthFirst.Value.Month, 1);

            DateTime endDate = new DateTime(
                dtpCreateYearLast.Value.Year,
                dtpCreateMonthLast.Value.Month,
                DateTime.DaysInMonth(dtpCreateYearLast.Value.Year, dtpCreateMonthLast.Value.Month));

            // Check if start date is after end date
            if (startDate > endDate)
            {
                MessageBox.Show("เดือนและปีเริ่มต้นต้องไม่มากกว่าเดือนและปีสิ้นสุด", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Reset to default values
                if (dtpCreateYearFirst.Focused || dtpCreateMonthFirst.Focused)
                {
                    dtpCreateYearFirst.Value = dtpCreateYearLast.Value;
                    dtpCreateMonthFirst.Value = dtpCreateMonthLast.Value;
                }
                else
                {
                    dtpCreateYearLast.Value = dtpCreateYearFirst.Value;
                    dtpCreateMonthLast.Value = dtpCreateMonthFirst.Value;
                }

                return false;
            }

            // Check if date range is too large (more than 2 years)
            int totalMonths = ((endDate.Year - startDate.Year) * 12) + (endDate.Month - startDate.Month) + 1;
            if (totalMonths > 24)
            {
                DialogResult result = MessageBox.Show(
                    "คุณกำลังเลือกช่วงเวลามากกว่า 2 ปี ซึ่งอาจทำให้การทำงานช้าลง\nต้องการดำเนินการต่อหรือไม่?",
                    "แจ้งเตือน",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    // Reset to more reasonable values
                    if (dtpCreateYearFirst.Focused || dtpCreateMonthFirst.Focused)
                    {
                        dtpCreateYearFirst.Value = dtpCreateYearLast.Value.AddYears(-1);
                    }
                    else
                    {
                        dtpCreateYearLast.Value = dtpCreateYearFirst.Value.AddYears(1);
                    }
                    return false;
                }
            }

            return true;
        }

        private void LoadReportData()
        {
            Cursor = Cursors.WaitCursor;

            try
            {
                using (SqlConnection connection = DBconfig.GetConnection())
                {
                    connection.Open();

                    // Get selected month and year values
                    int startMonth = dtpCreateMonthFirst.Value.Month;
                    int startYear = dtpCreateYearFirst.Value.Year;
                    int endMonth = dtpCreateMonthLast.Value.Month;
                    int endYear = dtpCreateYearLast.Value.Year;

                    // Create a query to get all laundry services as base data, including those with zero usage
                    string query = @"
                            WITH MonthsInRange AS (
                                -- Generate all months in the selected date range
                                SELECT 
                                    m.MonthNumber AS Month,
                                    m.YearNumber AS Year
                                FROM (
                                    SELECT 
                                        MONTH(DATEADD(MONTH, number, @StartDate)) AS MonthNumber,
                                        YEAR(DATEADD(MONTH, number, @StartDate)) AS YearNumber
                                    FROM master.dbo.spt_values
                                    WHERE 
                                        type = 'P' AND 
                                        number <= DATEDIFF(MONTH, @StartDate, @EndDate)
                                ) m
                            ),
                            ServiceItems AS (
                                -- Get all laundry services
                                SELECT 
                                    ls.ServiceType,
                                    ls.Gender,
                                    ls.ItemName,
                                    ls.ItemNumber
                                FROM LaundryService ls
                                WHERE ls.IsCancelled = N'ใช้งาน'
                            ),
                            SuccessfulItems AS (
                                -- Get successful receipt items (printed receipts, not canceled items)
                                SELECT 
                                    oi.ItemNumber,
                                    COUNT(ri.ReceiptItemID) AS UsageCount,
                                    MONTH(r.ReceiptDate) AS Month,
                                    YEAR(r.ReceiptDate) AS Year
                                FROM ReceiptItem ri
                                INNER JOIN Receipt r ON ri.ReceiptID = r.ReceiptID
                                INNER JOIN OrderItem oi ON ri.OrderItemID = oi.OrderItemID
                                WHERE r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว' 
                                    AND ri.IsCanceled = 0
                                    AND (
                                        (YEAR(r.ReceiptDate) = @StartYear AND MONTH(r.ReceiptDate) >= @StartMonth)
                                        OR (YEAR(r.ReceiptDate) = @EndYear AND MONTH(r.ReceiptDate) <= @EndMonth)
                                        OR (YEAR(r.ReceiptDate) > @StartYear AND YEAR(r.ReceiptDate) < @EndYear)
                                    )
                                GROUP BY oi.ItemNumber, MONTH(r.ReceiptDate), YEAR(r.ReceiptDate)
                            )

                            -- Cross join services with months to ensure all combinations exist
                            SELECT 
                                si.ServiceType AS 'ประเภทการซัก', 
                                si.Gender AS 'เพศ',
                                si.ItemName AS 'รายการ',
                                ISNULL(su.UsageCount, 0) AS 'จำนวนที่ใช้งาน',
                                mir.Month,
                                mir.Year
                            FROM ServiceItems si
                            CROSS JOIN MonthsInRange mir
                            LEFT JOIN SuccessfulItems su ON si.ItemNumber = su.ItemNumber 
                                                        AND mir.Month = su.Month 
                                                        AND mir.Year = su.Year
                            ORDER BY si.ServiceType, si.Gender, si.ItemName, mir.Year, mir.Month";

                    // Create the adapter and fill the data
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Set date parameters for full month range
                        DateTime startDate = new DateTime(startYear, startMonth, 1);
                        DateTime endDate = new DateTime(endYear, endMonth,
                            DateTime.DaysInMonth(endYear, endMonth)).AddDays(1).AddSeconds(-1);

                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);
                        command.Parameters.AddWithValue("@StartMonth", startMonth);
                        command.Parameters.AddWithValue("@StartYear", startYear);
                        command.Parameters.AddWithValue("@EndMonth", endMonth);
                        command.Parameters.AddWithValue("@EndYear", endYear);

                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        reportData = new DataTable();
                        adapter.Fill(reportData);

                        // Create pivot data for monthly report format
                        pivotData = CreatePivotTableWithGroups(reportData);
                    }
                }

                // Detach the current data source
                dgvReport.DataSource = null;

                // Clear any existing columns
                dgvReport.Columns.Clear();

                // In the LoadReportData method, after setting the DataSource:
                dgvReport.DataSource = pivotData;

                // Apply formatting after assigning the data source
                ApplyGroupingFormatting();

                // Force a refresh of the layout
                dgvReport.Refresh();

                // Update the form title to include date range
                string startMonthName = System.Globalization.CultureInfo.CurrentCulture
                    .DateTimeFormat.GetMonthName(dtpCreateMonthFirst.Value.Month);
                string endMonthName = System.Globalization.CultureInfo.CurrentCulture
                    .DateTimeFormat.GetMonthName(dtpCreateMonthLast.Value.Month);

                this.Text = $"สรุปรายการซักรีด - {startMonthName} {dtpCreateYearFirst.Value.Year} ถึง {endMonthName} {dtpCreateYearLast.Value.Year}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการโหลดข้อมูล: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private DataTable CreatePivotTableWithGroups(DataTable sourceTable)
        {
            // Create a new DataTable for the pivoted data with grouping
            DataTable pivotTable = new DataTable();

            // Add only a single grouping column for items
            pivotTable.Columns.Add("รายการ(ชิ้น/ครั้ง)", typeof(string));

            // Get all months in the date range and sort them chronologically
            List<Tuple<int, int, string>> monthsInOrder = new List<Tuple<int, int, string>>();
            int startMonth = dtpCreateMonthFirst.Value.Month;
            int startYear = dtpCreateYearFirst.Value.Year;
            int endMonth = dtpCreateMonthLast.Value.Month;
            int endYear = dtpCreateYearLast.Value.Year;

            // Create a list of all months in chronological order
            for (int year = startYear; year <= endYear; year++)
            {
                int monthStart = (year == startYear) ? startMonth : 1;
                int monthEnd = (year == endYear) ? endMonth : 12;

                for (int month = monthStart; month <= monthEnd; month++)
                {
                    // Store year, month, and formatted column name
                    string columnName = $"{month}/{year.ToString().Substring(2)}";
                    monthsInOrder.Add(new Tuple<int, int, string>(year, month, columnName));
                }
            }

            // Sort the months chronologically
            monthsInOrder.Sort((a, b) =>
            {
                int yearComparison = a.Item1.CompareTo(b.Item1);
                if (yearComparison != 0)
                    return yearComparison;
                return a.Item2.CompareTo(b.Item2);
            });

            // Add month columns in chronological order
            var months = new Dictionary<string, Tuple<int, int>>();
            foreach (var monthData in monthsInOrder)
            {
                string columnName = monthData.Item3;
                months.Add(columnName, new Tuple<int, int>(monthData.Item2, monthData.Item1));
                pivotTable.Columns.Add(columnName, typeof(object)); // Changed to object type to allow empty strings
            }

            // Add total column (always at the end)
            pivotTable.Columns.Add("รวมทั้งหมด", typeof(object)); // Changed to object type to allow empty strings

            // Get all service types and sort them so ซักแห้ง comes first
            var serviceTypes = sourceTable.AsEnumerable()
                .Select(row => row.Field<string>("ประเภทการซัก"))
                .Distinct()
                .ToList();

            // Custom sort to ensure ซักแห้ง comes before ซักน้ำ
            serviceTypes.Sort((a, b) =>
            {
                // If "ซักแห้ง" is present, it should come first
                if (a.Contains("ซักแห้ง")) return -1;
                if (b.Contains("ซักแห้ง")) return 1;
                // Otherwise use alphabetical order
                return string.Compare(a, b);
            });

            // Dictionary for the grand total
            Dictionary<string, int> grandTotalByMonth = new Dictionary<string, int>();

            // Initialize grand total for all months
            foreach (var monthCol in months)
            {
                string columnName = monthCol.Key;
                grandTotalByMonth[columnName] = 0;
            }
            int grandTotal = 0;

            // Process each service type in our sorted order
            foreach (string serviceType in serviceTypes)
            {
                // Dictionary to track service type totals
                Dictionary<string, int> serviceTypeTotalsByMonth = new Dictionary<string, int>();

                // Initialize service type totals
                foreach (var monthCol in months)
                {
                    serviceTypeTotalsByMonth[monthCol.Key] = 0;
                }
                int serviceTypeTotal = 0;

                // Filter for just this service type
                var serviceTypeRows = sourceTable.AsEnumerable()
                    .Where(row => row.Field<string>("ประเภทการซัก") == serviceType);

                // Add a service type header row
                DataRow serviceTypeHeaderRow = pivotTable.NewRow();
                serviceTypeHeaderRow["รายการ(ชิ้น/ครั้ง)"] = serviceType;

                // Set values to empty for all month columns for the service type header
                foreach (var monthCol in months)
                {
                    serviceTypeHeaderRow[monthCol.Key] = DBNull.Value;
                }
                serviceTypeHeaderRow["รวมทั้งหมด"] = DBNull.Value;
                pivotTable.Rows.Add(serviceTypeHeaderRow);

                // Group by gender within this service type
                var genderGroups = serviceTypeRows
                    .GroupBy(row => row.Field<string>("เพศ"))
                    .OrderBy(g => g.Key);

                foreach (var genderGroup in genderGroups)
                {
                    string gender = genderGroup.Key;

                    // Add a gender header row with indentation
                    DataRow genderHeaderRow = pivotTable.NewRow();
                    genderHeaderRow["รายการ(ชิ้น/ครั้ง)"] = $"    {gender}";

                    // Set values to empty for all month columns for the gender header
                    foreach (var monthCol in months)
                    {
                        genderHeaderRow[monthCol.Key] = DBNull.Value;
                    }
                    genderHeaderRow["รวมทั้งหมด"] = DBNull.Value;
                    pivotTable.Rows.Add(genderHeaderRow);

                    // Group by item name within this gender
                    var itemGroups = genderGroup
                        .GroupBy(row => row.Field<string>("รายการ"))
                        .OrderBy(g => g.Key);

                    foreach (var itemGroup in itemGroups)
                    {
                        string itemName = itemGroup.Key;

                        // Create item name with double indent
                        string combinedItemName = $"        {itemName}";

                        DataRow newRow = pivotTable.NewRow();
                        newRow["รายการ(ชิ้น/ครั้ง)"] = combinedItemName;

                        int totalUsage = 0;

                        // Fill in the usage for each month in chronological order
                        foreach (var monthCol in months)
                        {
                            string columnName = monthCol.Key;
                            int month = monthCol.Value.Item1;
                            int year = monthCol.Value.Item2;

                            // Find the matching row for this month/year if it exists
                            var monthData = itemGroup.FirstOrDefault(r =>
                                r.Field<int>("Month") == month &&
                                r.Field<int>("Year") == year);

                            int usage = 0;
                            if (monthData != null)
                            {
                                usage = Convert.ToInt32(monthData["จำนวนที่ใช้งาน"]);
                            }

                            // Set value to empty if usage is 0
                            newRow[columnName] = usage == 0 ? DBNull.Value : (object)usage;
                            totalUsage += usage;

                            // Add to service type total and grand total
                            serviceTypeTotalsByMonth[columnName] += usage;
                            grandTotalByMonth[columnName] += usage;
                        }

                        // Set total to empty if totalUsage is 0
                        newRow["รวมทั้งหมด"] = totalUsage == 0 ? DBNull.Value : (object)totalUsage;
                        serviceTypeTotal += totalUsage;
                        grandTotal += totalUsage;

                        pivotTable.Rows.Add(newRow);
                    }
                }

                // Add service type subtotal row at the end of each service type
                DataRow serviceTypeSubtotalRow = pivotTable.NewRow();
                serviceTypeSubtotalRow["รายการ(ชิ้น/ครั้ง)"] = $"รวม{serviceType}";

                // Add the monthly totals for this service type
                foreach (var monthCol in months)
                {
                    string columnName = monthCol.Key;
                    int monthTotal = serviceTypeTotalsByMonth[columnName];
                    serviceTypeSubtotalRow[columnName] = monthTotal == 0 ? DBNull.Value : (object)monthTotal;
                }

                serviceTypeSubtotalRow["รวมทั้งหมด"] = serviceTypeTotal == 0 ? DBNull.Value : (object)serviceTypeTotal;
                pivotTable.Rows.Add(serviceTypeSubtotalRow);
            }

            // Add grand total row
            DataRow grandTotalRow = pivotTable.NewRow();
            grandTotalRow["รายการ(ชิ้น/ครั้ง)"] = "รวมทั้งหมด";

            // Add the monthly totals for all service types
            foreach (var monthCol in months)
            {
                string columnName = monthCol.Key;
                int monthTotal = grandTotalByMonth[columnName];
                grandTotalRow[columnName] = monthTotal == 0 ? DBNull.Value : (object)monthTotal;
            }

            grandTotalRow["รวมทั้งหมด"] = grandTotal == 0 ? DBNull.Value : (object)grandTotal;

            pivotTable.Rows.Add(grandTotalRow);

            return pivotTable;
        }

        private void ApplyGroupingFormatting()
        {
            if (dgvReport.Rows.Count == 0) return;

            // Format all numeric columns to be right-aligned
            foreach (DataGridViewColumn col in dgvReport.Columns)
            {
                if (col.Index > 0) // Skip the first column (item description)
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            // Set column header styles
            dgvReport.ColumnHeadersDefaultCellStyle.BackColor = Color.LightGray;
            dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Angsana New", 16, FontStyle.Bold);

            // Colors for different row types
            Color serviceTypeHeaderColor = Color.LightBlue;
            Color genderHeaderColor = Color.LightCyan;
            Color serviceTypeSubtotalColor = Color.PaleGreen;
            Color grandTotalColor = Color.LightGoldenrodYellow;

            for (int i = 0; i < dgvReport.Rows.Count; i++)
            {
                string itemName = dgvReport.Rows[i].Cells["รายการ(ชิ้น/ครั้ง)"].Value?.ToString() ?? "";

                // Check if this is a subtotal or grand total row
                bool isServiceTypeSubtotal = itemName.StartsWith("รวม") && itemName != "รวมทั้งหมด";
                bool isGrandTotal = itemName == "รวมทั้งหมด";
                bool isServiceTypeHeader = !itemName.StartsWith(" ") && !itemName.StartsWith("รวม");
                bool isGenderHeader = itemName.StartsWith("    ") && !itemName.StartsWith("        ");
                bool isItemRow = itemName.StartsWith("        ");

                if (isGrandTotal)
                {
                    // Format grand total row
                    dgvReport.Rows[i].DefaultCellStyle.BackColor = grandTotalColor;
                    dgvReport.Rows[i].DefaultCellStyle.Font = new Font(dgvReport.DefaultCellStyle.Font, FontStyle.Bold);
                }
                else if (isServiceTypeSubtotal)
                {
                    // Format service type subtotal row
                    dgvReport.Rows[i].DefaultCellStyle.BackColor = serviceTypeSubtotalColor;
                    dgvReport.Rows[i].DefaultCellStyle.Font = new Font(dgvReport.DefaultCellStyle.Font, FontStyle.Bold);
                }
                else if (isServiceTypeHeader)
                {
                    // Format service type header row
                    dgvReport.Rows[i].DefaultCellStyle.BackColor = serviceTypeHeaderColor;
                    dgvReport.Rows[i].DefaultCellStyle.Font = new Font(dgvReport.DefaultCellStyle.Font, FontStyle.Bold);
                }
                else if (isGenderHeader)
                {
                    // Format gender header row
                    dgvReport.Rows[i].DefaultCellStyle.BackColor = genderHeaderColor;
                    dgvReport.Rows[i].DefaultCellStyle.Font = new Font(dgvReport.DefaultCellStyle.Font, FontStyle.Bold);
                }
                else
                {
                    // Regular data rows with alternating colors
                    dgvReport.Rows[i].DefaultCellStyle.BackColor = i % 2 == 0 ? Color.White : Color.WhiteSmoke;
                }

                // Make zeros appear as blank cells
                for (int j = 1; j < dgvReport.Columns.Count; j++)
                {
                    var cellValue = dgvReport.Rows[i].Cells[j].Value;
                    if (cellValue != null && cellValue != DBNull.Value)
                    {
                        if (cellValue.ToString() == "0")
                        {
                            dgvReport.Rows[i].Cells[j].Value = DBNull.Value;
                        }
                    }
                }
            }

            // Set column widths
            if (dgvReport.Columns.Count >= 1)
            {
                dgvReport.Columns[0].Width = 300; // Item description column

                // Set all month columns to equal width
                int monthColumnWidth = 60;
                for (int i = 1; i < dgvReport.Columns.Count; i++)
                {
                    dgvReport.Columns[i].Width = monthColumnWidth;
                }
            }

            // Add borders to the DataGridView
            dgvReport.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgvReport.BorderStyle = BorderStyle.Fixed3D;

            // Add visual separation between groups
            dgvReport.RowsDefaultCellStyle.SelectionBackColor = Color.LightSteelBlue;
            dgvReport.RowsDefaultCellStyle.SelectionForeColor = Color.Black;
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Updated btnPrint_Click method
        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (pivotData == null || pivotData.Rows.Count == 0)
            {
                MessageBox.Show("ไม่มีข้อมูลที่จะพิมพ์", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Reset pagination tracking variable before printing
            _currentPage = 0;

            PrintDocument printDocument = new PrintDocument();
            printDocument.PrintPage += PrintDocument_PrintPage;
            printDocument.EndPrint += PrintDocument_EndPrint;

            using (PrintDialog printDialog = new PrintDialog())
            {
                printDialog.Document = printDocument;
                printDialog.AllowSelection = false;
                printDialog.AllowSomePages = false;

                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Cursor = Cursors.WaitCursor;
                        printDocument.Print();
                    }
                    catch (Exception ex)
                    {
                        Cursor = Cursors.Default;
                        MessageBox.Show($"เกิดข้อผิดพลาดในการพิมพ์: {ex.Message}", "ข้อผิดพลาด",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Updated EndPrint method
        private void PrintDocument_EndPrint(object sender, PrintEventArgs e)
        {
            // Reset pagination tracking variable
            _currentPage = 0;

            // Reset cursor
            Cursor = Cursors.Default;

            // Use BeginInvoke to ensure UI operations happen on the UI thread
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Make sure our form is activated
                this.Activate();

                // Create a form for the message box that will always be on top
                Form messageForm = new Form();
                messageForm.TopMost = true;

                // Show the message box with our message form as owner
                MessageBox.Show(messageForm, "พิมพ์รายงานเรียบร้อยแล้ว", "สำเร็จ",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        // Updated PrintDocument_PrintPage method
        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            // Store the current page number for display before it might be incremented
            int currentPageForDisplay = _currentPage + 1; // Page numbers start from 1 for display

            // Define fonts
            Font titleFont = new Font("Angsana New", 16, FontStyle.Bold);
            Font headerFont = new Font("Angsana New", 12, FontStyle.Bold);
            Font dataFont = new Font("Angsana New", 11);
            Font groupFont = new Font("Angsana New", 11, FontStyle.Bold);

            // Page margins
            int leftMargin = 40;
            int topMargin = 40;
            int currentY = topMargin;

            // Page title
            string title = "สรุปรายการซักรีด";
            string startMonthName = System.Globalization.CultureInfo.CurrentCulture
                .DateTimeFormat.GetMonthName(dtpCreateMonthFirst.Value.Month);
            string endMonthName = System.Globalization.CultureInfo.CurrentCulture
                .DateTimeFormat.GetMonthName(dtpCreateMonthLast.Value.Month);
            string dateRange = $"เดือน {startMonthName} {dtpCreateYearFirst.Value.Year} ถึง {endMonthName} {dtpCreateYearLast.Value.Year}";

            // Draw report title (centered)
            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            e.Graphics.DrawString(title, titleFont, Brushes.Black, e.PageBounds.Width / 2, currentY, centerFormat);
            currentY += titleFont.Height;
            e.Graphics.DrawString(dateRange, headerFont, Brushes.Black, e.PageBounds.Width / 2, currentY, centerFormat);
            currentY += headerFont.Height + 10;

            // Calculate column widths for the table
            int itemColWidth = 180;  // Width for the item name column
            int monthColWidth = 60;  // Width for each month column

            // Calculate total table width
            int tableWidth = itemColWidth + (monthColWidth * (pivotData.Columns.Count - 1)); // Adjust calculation

            // Adjust if table is wider than page
            if (leftMargin + tableWidth > e.MarginBounds.Right)
            {
                int availableWidth = e.MarginBounds.Right - leftMargin;
                monthColWidth = (availableWidth - itemColWidth) / (pivotData.Columns.Count - 1);
                tableWidth = itemColWidth + (monthColWidth * (pivotData.Columns.Count - 1));
            }

            // Table header row height
            int headerHeight = 25;

            // Draw table header background
            Rectangle tableHeaderRect = new Rectangle(leftMargin, currentY, tableWidth, headerHeight);
            e.Graphics.FillRectangle(Brushes.LightGray, tableHeaderRect);
            e.Graphics.DrawRectangle(Pens.Black, tableHeaderRect);

            // Draw column headers
            int columnX = leftMargin;
            for (int i = 0; i < pivotData.Columns.Count; i++)
            {
                int columnWidth;
                if (i == 0) // Item name column
                    columnWidth = itemColWidth;
                else if (i == pivotData.Columns.Count - 1) // Total column
                    columnWidth = monthColWidth;
                else // Month columns
                    columnWidth = monthColWidth;

                Rectangle cellRect = new Rectangle(columnX, currentY, columnWidth, headerHeight);

                // Alignment for header cells
                StringFormat cellFormat = new StringFormat
                {
                    Alignment = (i == 0) ? StringAlignment.Near : StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                // Draw header text
                e.Graphics.DrawString(pivotData.Columns[i].ColumnName, headerFont, Brushes.Black,
                    new RectangleF(cellRect.X + 2, cellRect.Y, cellRect.Width - 4, cellRect.Height),
                    cellFormat);

                // Draw vertical line after this column
                if (i < pivotData.Columns.Count - 1)
                {
                    e.Graphics.DrawLine(Pens.Black, columnX + columnWidth, currentY,
                        columnX + columnWidth, currentY + headerHeight);
                }

                columnX += columnWidth;
            }

            currentY += headerHeight;

            // Calculate rows per page
            int rowHeight = 20;
            int availableHeight = e.MarginBounds.Height - (currentY - topMargin) - 50; // Reserve space for page number
            int rowsPerPage = availableHeight / rowHeight;

            // Calculate start and end row for current page
            int startRow = _currentPage * rowsPerPage;
            int endRow = Math.Min(startRow + rowsPerPage, pivotData.Rows.Count);

            for (int rowIndex = startRow; rowIndex < endRow; rowIndex++)
            {
                DataRow row = pivotData.Rows[rowIndex];
                string itemName = row["รายการ(ชิ้น/ครั้ง)"].ToString();

                // Determine if this is a group row
                bool isServiceTypeHeader = !itemName.StartsWith(" ") && !itemName.StartsWith("รวม");
                bool isGenderHeader = itemName.StartsWith("    ") && !itemName.StartsWith("        ");
                bool isSubtotal = itemName.StartsWith("รวม") && itemName != "รวมทั้งหมด";
                bool isGrandTotal = itemName == "รวมทั้งหมด";

                // Use appropriate font based on grouping
                Font rowFont = (isServiceTypeHeader || isGenderHeader || isSubtotal || isGrandTotal) ? groupFont : dataFont;
                Brush textBrush = Brushes.Black;

                // Draw the row with background color if it's a special row
                if (isServiceTypeHeader)
                {
                    Rectangle serviceTypeRect = new Rectangle(leftMargin, currentY, tableWidth, rowHeight);
                    e.Graphics.FillRectangle(Brushes.LightBlue, serviceTypeRect);
                }
                else if (isGenderHeader)
                {
                    Rectangle genderRect = new Rectangle(leftMargin, currentY, tableWidth, rowHeight);
                    e.Graphics.FillRectangle(Brushes.LightCyan, genderRect);
                }
                else if (isSubtotal)
                {
                    Rectangle subtotalRect = new Rectangle(leftMargin, currentY, tableWidth, rowHeight);
                    e.Graphics.FillRectangle(Brushes.PaleGreen, subtotalRect);
                }
                else if (isGrandTotal)
                {
                    Rectangle totalRect = new Rectangle(leftMargin, currentY, tableWidth, rowHeight);
                    e.Graphics.FillRectangle(Brushes.LightGoldenrodYellow, totalRect);
                }

                // Draw cells for this row
                columnX = leftMargin;
                for (int colIndex = 0; colIndex < pivotData.Columns.Count; colIndex++)
                {
                    int columnWidth;
                    if (colIndex == 0) // Item name column
                        columnWidth = itemColWidth;
                    else if (colIndex == pivotData.Columns.Count - 1) // Total column
                        columnWidth = monthColWidth;
                    else // Month columns
                        columnWidth = monthColWidth;

                    Rectangle cellRect = new Rectangle(columnX, currentY, columnWidth, rowHeight);

                    // Alignment based on column type
                    StringFormat cellFormat = new StringFormat
                    {
                        Alignment = (colIndex == 0) ? StringAlignment.Near : StringAlignment.Far,
                        LineAlignment = StringAlignment.Center
                    };

                    // Get cell value - use index-based access to avoid column name issues
                    string value = row[colIndex].ToString();

                    // Draw cell value
                    e.Graphics.DrawString(value, rowFont, textBrush,
                        new RectangleF(cellRect.X + 2, cellRect.Y, cellRect.Width - 4, cellRect.Height),
                        cellFormat);

                    // Draw cell borders
                    e.Graphics.DrawRectangle(Pens.Black, cellRect);

                    columnX += columnWidth;
                }

                currentY += rowHeight;
            }

            // Calculate total pages for displaying at the bottom
            int totalPages = (int)Math.Ceiling((double)pivotData.Rows.Count / rowsPerPage);

            // Draw page number at bottom
            string pageText = $"หน้า {currentPageForDisplay} จาก {totalPages}";
            SizeF pageTextSize = e.Graphics.MeasureString(pageText, dataFont);
            e.Graphics.DrawString(pageText, dataFont, Brushes.Black,
                e.PageBounds.Width / 2 - pageTextSize.Width / 2,
                e.PageBounds.Height - 30);

            // Check if we have more pages to print - MUST BE AT THE END
            if (endRow < pivotData.Rows.Count)
            {
                _currentPage++; // Increment page counter for next page
                e.HasMorePages = true; // Signal that we need to print more pages
            }
            else
            {
                e.HasMorePages = false; // No more pages to print
            }
        }

        private void btnExcel_Click(object sender, EventArgs e)
        {
            try
            {
                if (pivotData == null || pivotData.Rows.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะส่งออก", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV Files|*.csv|Excel Files|*.xls";
                    sfd.Title = "บันทึกไฟล์รายงานบริการซักรีด";

                    // Get date range string for default filename
                    string startMonth = System.Globalization.CultureInfo.CurrentCulture
                        .DateTimeFormat.GetMonthName(dtpCreateMonthFirst.Value.Month);
                    string endMonth = System.Globalization.CultureInfo.CurrentCulture
                        .DateTimeFormat.GetMonthName(dtpCreateMonthLast.Value.Month);
                    string dateRangeStr = $"{startMonth}_{dtpCreateYearFirst.Value.Year}_ถึง_{endMonth}_{dtpCreateYearLast.Value.Year}";
                    sfd.FileName = $"รายงานบริการซักรีด_{dateRangeStr}";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        // Show waiting cursor
                        Cursor = Cursors.WaitCursor;

                        // Create a StringBuilder to build CSV content
                        StringBuilder csv = new StringBuilder();

                        // Add UTF-8 BOM for Thai character support
                        csv.Append("\uFEFF");

                        // Add CSV header row
                        List<string> headers = new List<string>();
                        foreach (DataColumn column in pivotData.Columns)
                        {
                            headers.Add($"\"{column.ColumnName}\"");
                        }
                        csv.AppendLine(string.Join(",", headers));

                        foreach (DataRow row in pivotData.Rows)
                        {
                            List<string> cells = new List<string>();
                            for (int i = 0; i < pivotData.Columns.Count; i++)
                            {
                                string value = row[i].ToString().Replace("\"", "\"\"");
                                cells.Add($"\"{value}\"");
                            }
                            csv.AppendLine(string.Join(",", cells));
                        }

                        // Add summary information
                        csv.AppendLine();
                        csv.AppendLine($"\"สรุปรายงานบริการซักรีด\"");
                        csv.AppendLine($"\"ช่วงเดือน: {startMonth} {dtpCreateYearFirst.Value.Year} ถึง {endMonth} {dtpCreateYearLast.Value.Year}\"");
                        csv.AppendLine($"\"จำนวนรายการทั้งหมด: {pivotData.Rows.Count} รายการ\"");
                        csv.AppendLine($"\"ออกรายงานเมื่อ: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}\"");

                        // Save to file
                        File.WriteAllText(sfd.FileName, csv.ToString());

                        Cursor = Cursors.Default;
                        MessageBox.Show("ส่งออกข้อมูลเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show($"เกิดข้อผิดพลาดในการส่งออกข้อมูล: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Service_Report_Load(object sender, EventArgs e)
        {
            InitializeDataGridView();
            LoadReportData(); // Load data with initial date range
        }
    }
}
