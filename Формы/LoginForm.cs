using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Taskora.БазаДанных;
using Taskora.Логика;

namespace Taskora.Формы
{
    public class LoginForm : Form
    {
        private RoundedPanel card;
        private TextBox loginBox;
        private TextBox passwordBox;
        private Label errorLabel;
        private Button loginButton;
        private Button registerButton;

        public LoginForm()
        {
            InitializeUi();
        }

        private void InitializeUi()
        {
            Icon = Ui.LoadIcon();
            Text = "Taskora — Вход";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Ui.Background;
            Font = Ui.GetFont(10);

            card = new RoundedPanel
            {
                Location = new Point(40, 30),
                Width = 350
            };
            Image logoImage = Ui.LoadLogo();

            Control logo;

            if (logoImage != null)
            {
                logo = new PictureBox
                {
                    Size = new Size(76, 76),
                    Location = new Point((350 - 76) / 2, 22),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Image = logoImage
                };
            }
            else
            {
                Panel drawnLogo = new Panel
                {
                    Size = new Size(64, 64),
                    Location = new Point((350 - 64) / 2, 28),
                    BackColor = Color.Transparent
                };
                drawnLogo.Paint += Logo_Paint;
                logo = drawnLogo;
            }

            Label title = Ui.CreateLabel("Taskora", 24, FontStyle.Bold, Ui.Text, false);
            Size titleSize = TextRenderer.MeasureText(title.Text, title.Font);
            title.Size = titleSize;
            title.Location = new Point((350 - titleSize.Width) / 2, 104);

            Label subtitle = Ui.CreateLabel("Личный менеджер задач", 10, FontStyle.Regular, Ui.Muted, false);
            Size subtitleSize = TextRenderer.MeasureText(subtitle.Text, subtitle.Font);
            subtitle.Size = subtitleSize;
            subtitle.Location = new Point((350 - subtitleSize.Width) / 2, title.Bottom + 6);

            int y = subtitle.Bottom + 26;

            Label loginLabel = Ui.CreateLabel("Логин", 10, FontStyle.Bold, Ui.Text);
            loginLabel.Location = new Point(40, y);
            y += 24;

            loginBox = Ui.CreateTextBox(270);
            loginBox.Location = new Point(40, y);
            y += 34;

            Label passwordLabel = Ui.CreateLabel("Пароль", 10, FontStyle.Bold, Ui.Text);
            passwordLabel.Location = new Point(40, y);
            y += 24;

            passwordBox = Ui.CreateTextBox(270);
            passwordBox.Location = new Point(40, y);
            passwordBox.PasswordChar = '•';
            passwordBox.UseSystemPasswordChar = true;

            errorLabel = new Label
            {
                MaximumSize = new Size(270, 0),
                AutoSize = true,
                Font = Ui.GetFont(9),
                ForeColor = Ui.Danger,
                Visible = false
            };

            loginButton = Ui.CreateButton("Войти", Ui.Primary, new Size(270, 44));
            loginButton.Click += LoginButton_Click;

            registerButton = Ui.CreateOutlineButton("Создать аккаунт", Ui.Primary, new Size(270, 40));
            registerButton.Click += RegisterButton_Click;

            loginBox.TextChanged += (s, e) => ShowError("");
            passwordBox.TextChanged += (s, e) => ShowError("");

            card.Controls.Add(logo);
            card.Controls.Add(title);
            card.Controls.Add(subtitle);
            card.Controls.Add(loginLabel);
            card.Controls.Add(loginBox);
            card.Controls.Add(passwordLabel);
            card.Controls.Add(passwordBox);
            card.Controls.Add(errorLabel);
            card.Controls.Add(loginButton);
            card.Controls.Add(registerButton);

            Controls.Add(card);

            AcceptButton = loginButton;
            ActiveControl = loginBox;

            ApplyLayout();
        }

        private void ApplyLayout()
        {
            int y = passwordBox.Bottom + 16;

            if (errorLabel.Visible)
            {
                errorLabel.Location = new Point(40, y);
                y += errorLabel.Height + 12;
            }

            loginButton.Location = new Point(40, y);
            y += loginButton.Height + 10;

            registerButton.Location = new Point(40, y);
            y += registerButton.Height + 30;

            card.Height = y;
            ClientSize = new Size(430, y + 60);
        }

        private void ShowError(string message)
        {
            errorLabel.Text = message;
            errorLabel.Visible = !string.IsNullOrEmpty(message);
            ApplyLayout();
        }

        private void Logo_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, 63, 63);

            using GraphicsPath path = Ui.CreateRoundPath(rect, 18);
            using SolidBrush brush = new SolidBrush(Ui.Primary);
            e.Graphics.FillPath(brush, path);

            using Pen pen = new Pen(Color.White, 5)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            e.Graphics.DrawLines(pen, new[]
            {
                new Point(17, 34),
                new Point(27, 44),
                new Point(46, 22)
            });
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            ShowError("");

            string login = loginBox.Text.Trim();
            string password = passwordBox.Text;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите логин и пароль.");
                return;
            }

            try
            {
                var user = UserService.Authenticate(login, password);

                if (user == null)
                {
                    ShowError("Неверный логин или пароль.");
                    return;
                }

                Session.CurrentUser = user;

                Hide();

                MainForm mainForm = new MainForm();
                mainForm.FormClosed += (s, args) => Close();
                mainForm.Show();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка: " + ex.Message);
            }
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            using RegisterForm form = new RegisterForm();

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                loginBox.Text = form.NewLogin;
                passwordBox.Text = "";
                passwordBox.Focus();
            }
        }
    }
}