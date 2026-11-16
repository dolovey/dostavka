using System;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace DeliveryApp
{
    // ===================== СОСТОЯНИЕ (SQLITE) =====================
    static class AppState
    {
        // ЖЕСТКИЙ ПУТЬ: база создастся прямо в папке проекта (рядом с этим файлом)
        private const string ConnectionString = @"Data Source=C:\Users\Daniil\MyWinFormsApp\delivery.db;";

        public static DataRow CurrentUser = null;
        public static DataTable Users = new DataTable("Users");
        public static DataTable Admins = new DataTable("Admins");
        public static DataTable Orders = new DataTable("Orders");

        public static void Init()
        {
            SetupTables();
            CreateTablesIfNotExist();
            LoadDataFromDb();
        }

        private static void SetupTables()
        {
            Users.Columns.Add("Id", typeof(int));
            Users.Columns.Add("Name", typeof(string));
            Users.Columns.Add("Email", typeof(string));
            Users.Columns.Add("Password", typeof(string));
            Users.PrimaryKey = new[] { Users.Columns["Id"] };

            Admins.Columns.Add("Id", typeof(int));
            Admins.Columns.Add("Login", typeof(string));
            Admins.Columns.Add("Password", typeof(string));
            Admins.PrimaryKey = new[] { Admins.Columns["Id"] };

            Orders.Columns.Add("Id", typeof(int));
            Orders.Columns.Add("Client", typeof(string));
            Orders.Columns.Add("Products", typeof(string));
            Orders.Columns.Add("Address", typeof(string));
            Orders.Columns.Add("LeaveAtDoor", typeof(string));
            Orders.Columns.Add("CreatedAt", typeof(DateTime));
            Orders.PrimaryKey = new[] { Orders.Columns["Id"] };
        }

        private static void CreateTablesIfNotExist()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Email TEXT UNIQUE, Password TEXT);
                        CREATE TABLE IF NOT EXISTS Admins (Id INTEGER PRIMARY KEY AUTOINCREMENT, Login TEXT UNIQUE, Password TEXT);
                        CREATE TABLE IF NOT EXISTS Orders (Id INTEGER PRIMARY KEY AUTOINCREMENT, Client TEXT, Products TEXT, Address TEXT, LeaveAtDoor TEXT, CreatedAt TEXT);
                        INSERT OR IGNORE INTO Admins (Login, Password) VALUES ('admin', 'admin');
                    ";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void LoadDataFromDb()
        {
            Users.Clear(); Admins.Clear(); Orders.Clear();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                
                using (var cmd = new SqliteCommand("SELECT * FROM Users", conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) Users.Rows.Add(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));

                using (var cmd = new SqliteCommand("SELECT * FROM Admins", conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) Admins.Rows.Add(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));

                using (var cmd = new SqliteCommand("SELECT * FROM Orders", conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) Orders.Rows.Add(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), DateTime.Parse(reader.GetString(5)));
            }
        }

        // --- СОХРАНЕНИЕ В БД ---
        public static void SaveUser(string name, string email, string password)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Users (Name, Email, Password) VALUES (@n, @e, @p);";
                    cmd.Parameters.AddWithValue("n", name);
                    cmd.Parameters.AddWithValue("e", email);
                    cmd.Parameters.AddWithValue("p", password);
                    cmd.ExecuteNonQuery();
                }
            }
            LoadDataFromDb();
        }

        public static void SaveOrder(string client, string products, string address, string leave, DateTime dt)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Orders (Client, Products, Address, LeaveAtDoor, CreatedAt) VALUES (@c, @p, @a, @l, @dt);";
                    cmd.Parameters.AddWithValue("c", client);
                    cmd.Parameters.AddWithValue("p", products);
                    cmd.Parameters.AddWithValue("a", address);
                    cmd.Parameters.AddWithValue("l", leave);
                    cmd.Parameters.AddWithValue("dt", dt.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
            LoadDataFromDb();
        }
    }

    // ===================== ГЛАВНОЕ ОКНО =====================
    public class MainForm : Form
    {
        public MainForm()
        {
            this.FormClosed += (s, e) => Application.Exit();
            Text = "Доставка еды";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(480, 340);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 11f);

            var title = new Label { Text = "Доставка еды", Font = new Font("Segoe UI", 22f, FontStyle.Bold), Dock = DockStyle.Top, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            var subtitle = new Label { Text = "Чтобы оформить заказ, зарегистрируйтесь\nили войдите в аккаунт.", Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter };

            var btnReg = new Button { Text = "Регистрация", Size = new Size(240, 40), Cursor = Cursors.Hand };
            btnReg.Click += (s, e) => { new RegisterForm().Show(); Hide(); };

            var btnLogin = new Button { Text = "Войти", Size = new Size(240, 40), Cursor = Cursors.Hand };
            btnLogin.Click += (s, e) => { new LoginForm().Show(); Hide(); };

            var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Location = new Point(120, 200) };
            panel.Controls.Add(btnReg);
            panel.Controls.Add(btnLogin);

            Controls.Add(subtitle); Controls.Add(title); Controls.Add(panel);
        }
    }

    // ===================== РЕГИСТРАЦИЯ =====================
    public class RegisterForm : Form
    {
        private Panel panelStep1, panelStep2, panelStep3;
        private TextBox txtName, txtEmail, txtPassword, txtPasswordRepeat;
        private Label lblConfirm;
        private string _pendingName = "", _pendingEmail = "";

        public RegisterForm()
        {
             this.FormClosed += (s, e) => Application.Exit();
            Text = "Регистрация"; StartPosition = FormStartPosition.CenterScreen; ClientSize = new Size(480, 340); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; Font = new Font("Segoe UI", 11f);
            BuildStep1(); BuildStep2(); BuildStep3();
            Controls.Add(panelStep1); Controls.Add(panelStep2); Controls.Add(panelStep3);
            ShowStep(1);
        }

        private void ShowStep(int step)
        {
             this.FormClosed += (s, e) => Application.Exit();
            panelStep1.Visible = step == 1; panelStep2.Visible = step == 2; panelStep3.Visible = step == 3;
            if (step == 1) txtName.Focus();
            if (step == 2) txtPassword.Focus();
            if (step == 3) UpdateConfirmLabel();
        }

        private void BuildStep1()
        {
             this.FormClosed += (s, e) => Application.Exit();
            panelStep1 = new Panel { Dock = DockStyle.Fill };
            var btnBack = new Button { Text = "Назад", Location = new Point(140, 240), Width = 100 };
            btnBack.Click += (s, e) => { new MainForm().Show(); Hide(); };
            var btnNext = new Button { Text = "Далее", Location = new Point(250, 240), Width = 100 };
            btnNext.Click += (s, e) => ValidateStep1();

            txtName = new TextBox { Location = new Point(140, 67), Width = 280 };
            txtEmail = new TextBox { Location = new Point(140, 112), Width = 280 };

            panelStep1.Controls.AddRange(new Control[] { new Label { Text = "Шаг 1 из 3", Location = new Point(30, 20), AutoSize = true },
                new Label { Text = "Имя:", Location = new Point(30, 70), AutoSize = true }, txtName,
                new Label { Text = "Email:", Location = new Point(30, 115), AutoSize = true }, txtEmail, btnBack, btnNext });
        }

        private void ValidateStep1()
        {
             this.FormClosed += (s, e) => Application.Exit();
            var name = txtName.Text.Trim(); var email = txtEmail.Text.Trim();
            if (name.Length < 2) { MessageBox.Show("Имя слишком короткое"); return; }
            if (!Regex.IsMatch(email, @"^[\w\.-]+@[\w\.-]+\.\w+$")) { MessageBox.Show("Неверный email"); return; }
            if (AppState.Users.Select("Email = '" + email.Replace("'", "''") + "'").Length > 0) { MessageBox.Show("Email уже занят"); return; }
            _pendingName = name; _pendingEmail = email; ShowStep(2);
        }

        private void BuildStep2()
        {
             this.FormClosed += (s, e) => Application.Exit();
            panelStep2 = new Panel { Dock = DockStyle.Fill };
            var btnBack = new Button { Text = "Назад", Location = new Point(140, 240), Width = 100 };
            btnBack.Click += (s, e) => ShowStep(1);
            var btnNext = new Button { Text = "Далее", Location = new Point(250, 240), Width = 100 };
            btnNext.Click += (s, e) => ValidateStep2();

            txtPassword = new TextBox { Location = new Point(140, 67), Width = 280, UseSystemPasswordChar = true };
            txtPasswordRepeat = new TextBox { Location = new Point(140, 112), Width = 280, UseSystemPasswordChar = true };

            panelStep2.Controls.AddRange(new Control[] { new Label { Text = "Шаг 2 из 3", Location = new Point(30, 20), AutoSize = true },
                new Label { Text = "Пароль:", Location = new Point(30, 70), AutoSize = true }, txtPassword,
                new Label { Text = "Повтор:", Location = new Point(30, 115), AutoSize = true }, txtPasswordRepeat, btnBack, btnNext });
        }

        private void ValidateStep2()
        {
             this.FormClosed += (s, e) => Application.Exit();
            if (txtPassword.Text.Length < 6) { MessageBox.Show("Пароль минимум 6 символов"); return; }
            if (txtPassword.Text != txtPasswordRepeat.Text) { MessageBox.Show("Пароли не совпадают"); return; }
            ShowStep(3);
        }

        private void BuildStep3()
        {
             this.FormClosed += (s, e) => Application.Exit();
            panelStep3 = new Panel { Dock = DockStyle.Fill };
            var btnBack = new Button { Text = "Назад", Location = new Point(140, 240), Width = 100 };
            btnBack.Click += (s, e) => ShowStep(2);
            var btnFinish = new Button { Text = "Завершить", Location = new Point(250, 240), Width = 100 };
            btnFinish.Click += (s, e) => Finish();

            lblConfirm = new Label { Location = new Point(30, 70), AutoSize = true };

            panelStep3.Controls.AddRange(new Control[] { new Label { Text = "Шаг 3 из 3", Location = new Point(30, 20), AutoSize = true }, lblConfirm, btnBack, btnFinish });
        }

        private void UpdateConfirmLabel() => lblConfirm.Text = $"Имя: {_pendingName}\nEmail: {_pendingEmail}\nПароль: {new string('•', txtPassword.Text.Length)}";

        private void Finish()
        {
            try
            {
                AppState.SaveUser(_pendingName, _pendingEmail, txtPassword.Text); // СОХРАНЕНИЕ В БД
                MessageBox.Show("Регистрация успешна!");
                new LoginForm().Show(); Hide();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }

    // ===================== ВХОД =====================
    public class LoginForm : Form
    {
        public LoginForm()
        {
             this.FormClosed += (s, e) => Application.Exit();
            Text = "Вход"; StartPosition = FormStartPosition.CenterScreen; ClientSize = new Size(480, 340); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; Font = new Font("Segoe UI", 11f);

            var txtEmail = new TextBox { Location = new Point(140, 92), Width = 280 };
            var txtPass = new TextBox { Location = new Point(140, 137), Width = 280, UseSystemPasswordChar = true };

            var btnBack = new Button { Text = "Назад", Location = new Point(140, 230), Width = 100 };
            btnBack.Click += (s, e) => { new MainForm().Show(); Hide(); };

            var btnLogin = new Button { Text = "Войти", Location = new Point(250, 230), Width = 100 };
            btnLogin.Click += (s, e) => {
                if (txtEmail.Text == "admin" && txtPass.Text == "admin") { AppState.CurrentUser = null; new AdminForm().Show(); Hide(); return; }
                var rows = AppState.Users.Select("Email = '" + txtEmail.Text.Replace("'", "''") + "' AND Password = '" + txtPass.Text.Replace("'", "''") + "'");
                if (rows.Length == 0) { MessageBox.Show("Неверный email или пароль"); return; }
                AppState.CurrentUser = rows[0]; new OrderForm().Show(); Hide();
            };

            AcceptButton = btnLogin;

            Controls.AddRange(new Control[] { new Label { Text = "Вход в аккаунт", Location = new Point(30, 30), AutoSize = true },
                new Label { Text = "Email:", Location = new Point(30, 95), AutoSize = true }, txtEmail,
                new Label { Text = "Пароль:", Location = new Point(30, 140), AutoSize = true }, txtPass, btnBack, btnLogin });
        }
    }

    // ===================== ЗАКАЗ =====================
    public class OrderForm : Form
    {
        public OrderForm()
        {
             this.FormClosed += (s, e) => Application.Exit();
            Text = "Оформление заказа"; StartPosition = FormStartPosition.CenterScreen; ClientSize = new Size(560, 500); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; Font = new Font("Segoe UI", 11f);

            var txtProducts = new RichTextBox { Location = new Point(30, 95), Size = new Size(500, 160) };
            var txtAddress = new TextBox { Location = new Point(30, 295), Width = 500 };
            var chkDoor = new CheckBox { Text = "Оставить у двери", Location = new Point(30, 335), AutoSize = true };

            var btnBack = new Button { Text = "Назад", Location = new Point(300, 400), Width = 100 };
            btnBack.Click += (s, e) => { AppState.CurrentUser = null; new MainForm().Show(); Hide(); };

            var btnSubmit = new Button { Text = "Оформить", Location = new Point(410, 400), Width = 120 };
            btnSubmit.Click += (s, e) => {
                if (txtProducts.Text.Length == 0 || txtAddress.Text.Length < 5) { MessageBox.Show("Заполните данные"); return; }
                try {
                    AppState.SaveOrder(AppState.CurrentUser["Name"].ToString(), txtProducts.Text.Replace("\n", ", "), txtAddress.Text, chkDoor.Checked ? "да" : "нет", DateTime.Now); // СОХРАНЕНИЕ В БД
                    MessageBox.Show("Заказ оформлен!");
                } catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            Controls.AddRange(new Control[] { new Label { Text = $"Здравствуйте, {AppState.CurrentUser?["Name"]}!", Location = new Point(30, 20), AutoSize = true },
                new Label { Text = "Продукты:", Location = new Point(30, 70), AutoSize = true }, txtProducts,
                new Label { Text = "Адрес:", Location = new Point(30, 270), AutoSize = true }, txtAddress, chkDoor, btnBack, btnSubmit });
        }
    }

    // ===================== АДМИНКА (ИСПРАВЛЕННАЯ) =====================
    public class AdminForm : Form
    {
        private DataGridView gridUsers, gridOrders;
        private MonthCalendar calendar;
        private Label lblFilter;

        public AdminForm()
        {
             this.FormClosed += (s, e) => Application.Exit();
            Text = "Панель администратора";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 760);
            MinimumSize = new Size(1000, 760);
            Font = new Font("Segoe UI", 11f);

            // --- Верхняя панель с кнопками ---
            var top = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(15, 10, 15, 10) };
            var lblTitle = new Label
            {
                Text = "Панель администратора",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 15)
            };

            var btnRefresh = new Button
            {
                Text = "Обновить", Width = 110, Height = 32,
                Cursor = Cursors.Hand, FlatStyle = FlatStyle.System,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.Click += (s, e) => RefreshAll();

            var btnLogout = new Button
            {
                Text = "Выйти", Width = 90, Height = 32,
                Cursor = Cursors.Hand, FlatStyle = FlatStyle.System,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnLogout.Click += (s, e) => { new MainForm().Show(); Hide(); };

            var btnBack = new Button
            {
                Text = "Назад", Width = 90, Height = 32,
                Cursor = Cursors.Hand, FlatStyle = FlatStyle.System,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBack.Click += (s, e) => { new LoginForm().Show(); Hide(); };

            top.Controls.AddRange(new Control[] { lblTitle, btnBack, btnLogout, btnRefresh });

            // Красивое выравнивание кнопок справа при изменении размера окна
            top.Resize += (s, e) =>
            {
                btnRefresh.Left = top.Width - btnRefresh.Width - 15;
                btnLogout.Left  = btnRefresh.Left - btnLogout.Width - 8;
                btnBack.Left    = btnLogout.Left - btnBack.Width - 8;
                btnRefresh.Top = btnLogout.Top = btnBack.Top = 15;
            };

            // --- Таблица пользователей ---
            var lblUsers = new Label { Text = "Пользователи", Font = new Font("Segoe UI", 12f, FontStyle.Bold), Dock = DockStyle.Top, Height = 30, Padding = new Padding(15, 0, 0, 0) };
            gridUsers = BuildGrid();
            gridUsers.Dock = DockStyle.Top;
            gridUsers.Height = 200;

            // --- Нижняя часть: Заказы слева, Календарь справа ---
            var bottom = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

            var leftPanel = new Panel { Dock = DockStyle.Fill };
            var lblOrders = new Label { Text = "Заказы", Font = new Font("Segoe UI", 12f, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
            lblFilter = new Label { Text = "все заказы", ForeColor = Color.Gray, Dock = DockStyle.Top, Height = 20 };
            
            gridOrders = BuildGrid();
            gridOrders.Dock = DockStyle.Fill;

            leftPanel.Controls.Add(gridOrders);
            leftPanel.Controls.Add(lblFilter);
            leftPanel.Controls.Add(lblOrders);

            // Правая панель с календарем
            var rightPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Width = 260,
                Dock = DockStyle.Right,
                Padding = new Padding(10)
            };
            var lblCal = new Label { Text = "Календарь", Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true };
            
            calendar = new MonthCalendar { MaxSelectionCount = 31 };
            calendar.DateChanged += (s, e) => FilterOrders();
            
            var btnReset = new Button { Text = "Сбросить фильтр", Width = 230, Height = 32, Cursor = Cursors.Hand, FlatStyle = FlatStyle.System };
            btnReset.Click += (s, e) =>
            {
                calendar.SetSelectionRange(DateTime.Today, DateTime.Today);
                FilterOrders();
            };

            rightPanel.Controls.AddRange(new Control[] { lblCal, calendar, btnReset });

            bottom.Controls.Add(leftPanel);
            bottom.Controls.Add(rightPanel);

            Controls.Add(bottom);
            Controls.Add(gridUsers);
            Controls.Add(lblUsers);
            Controls.Add(top);

            // Загружаем данные при открытии
            RefreshAll();
        }

        // Метод для красивой настройки таблиц
        private DataGridView BuildGrid()
        {
            var g = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 10f),
                BackgroundColor = Color.White
            };
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(232, 232, 232);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            g.EnableHeadersVisualStyles = false;
            return g;
        }

        // Обновление всех данных на экране
        private void RefreshAll()
        {
             this.FormClosed += (s, e) => Application.Exit();
            // 1. Перезагружаем данные из SQLite (на случай, если кто-то сделал заказ)
            AppState.LoadDataFromDb();

            // 2. Заполняем таблицу пользователей
            var usersTable = new DataTable();
            usersTable.Columns.Add("Имя");
            usersTable.Columns.Add("Email");
            foreach (DataRow r in AppState.Users.Rows)
                usersTable.Rows.Add(r["Name"], r["Email"]);
            gridUsers.DataSource = usersTable;

            // 3. Фильтруем заказы
            FilterOrders();
        }

        // Фильтрация заказов по календарю
        private void FilterOrders()
        {
             this.FormClosed += (s, e) => Application.Exit();
            var start = calendar.SelectionStart.Date;
            var end = calendar.SelectionEnd.Date;

            var table = new DataTable();
            table.Columns.Add("Клиент");
            table.Columns.Add("Продукты");
            table.Columns.Add("Адрес");
            table.Columns.Add("У двери");
            table.Columns.Add("Время");

            // Собираем подходящие заказы
            var list = new System.Collections.Generic.List<DataRow>();
            foreach (DataRow r in AppState.Orders.Rows)
            {
                var dt = (DateTime)r["CreatedAt"];
                if (dt.Date >= start && dt.Date <= end) list.Add(r);
            }
            
            // Сортируем: самые новые сверху
            list.Sort((a, b) => ((DateTime)b["CreatedAt"]).CompareTo((DateTime)a["CreatedAt"]));

            foreach (var r in list)
                table.Rows.Add(r["Client"], r["Products"], r["Address"], r["LeaveAtDoor"],
                               ((DateTime)r["CreatedAt"]).ToString("yyyy-MM-dd HH:mm"));

            gridOrders.DataSource = table;

            // Меняем текст над таблицей
            if (start == end)
                lblFilter.Text = $"дата: {start:yyyy-MM-dd} (найдено: {list.Count})";
            else
                lblFilter.Text = $"диапазон: {start:yyyy-MM-dd} — {end:yyyy-MM-dd} (найдено: {list.Count})";
        }
    }
}