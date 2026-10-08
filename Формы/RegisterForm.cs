using System;
using System.Drawing;
using System.Windows.Forms;
using Taskora.Логика;

namespace Taskora.Формы
{
    public class RegisterForm : Form
    {
        private TextBox nameBox;
        private TextBox loginBox;
        private TextBox passwordBox;
        private TextBox confirmPasswordBox;
        private Label errorLabel;

        public string NewLogin { get; private set; } = "";

        public RegisterForm()
        {
            InitializeUi();
        }

        private void InitializeUi()
        {
            Icon = Ui.LoadIcon();
            Text = "Taskora — Регистрация";
            ClientSize = new Size(430, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Ui.Background;
            Font = Ui.GetFont(10);

            RoundedPanel card = new RoundedPanel
            {
                Size = new Size(350, 490),
                Location = new Point(40, 30)
            };

            Label title = new Label
            {
                Text = "Регистрация",
                Bounds = new Rectangle(0, 20, card.Width, 32),
                Font = Ui.GetFont(18, FontStyle.Bold),
                ForeColor = Ui.Text,
                TextAlign = ContentAlignment.MiddleCenter
            };

            int y = 68;
            int step = 38;

            Label nameLabel = Ui.CreateLabel("Имя", 10, FontStyle.Bold, Ui.Text);
            nameLabel.Location = new Point(40, y);
            y += 22;

            nameBox = Ui.CreateTextBox(270);
            nameBox.Location = new Point(40, y);
            y += step;

            Label loginLabel = Ui.CreateLabel("Логин", 10, FontStyle.Bold, Ui.Text);
            loginLabel.Location = new Point(40, y);
            y += 22;

            loginBox = Ui.CreateTextBox(270);
            loginBox.Location = new Point(40, y);
            y += step;

            Label passwordLabel = Ui.CreateLabel("Пароль", 10, FontStyle.Bold, Ui.Text);
            passwordLabel.Location = new Point(40, y);
            y += 22;

            passwordBox = Ui.CreateTextBox(270);
            passwordBox.Location = new Point(40, y);
            passwordBox.PasswordChar = '•';
            passwordBox.UseSystemPasswordChar = true;
            y += step;

            Label confirmLabel = Ui.CreateLabel("Повторите пароль", 10, FontStyle.Bold, Ui.Text);
            confirmLabel.Location = new Point(40, y);
            y += 22;

            confirmPasswordBox = Ui.CreateTextBox(270);
            confirmPasswordBox.Location = new Point(40, y);
            confirmPasswordBox.PasswordChar = '•';
            confirmPasswordBox.UseSystemPasswordChar = true;
            y += 48;

            errorLabel = new Label
            {
                Bounds = new Rectangle(40, y, 270, 40),
                Font = Ui.GetFont(9),
                ForeColor = Ui.Danger
            };
            y += 45;

            Button saveButton = Ui.CreateButton("Зарегистрироваться", Ui.Primary, new Size(270, 44));
            saveButton.Location = new Point(40, y);
            saveButton.Click += SaveButton_Click;

            card.Controls.Add(title);
            card.Controls.Add(nameLabel);
            card.Controls.Add(nameBox);
            card.Controls.Add(loginLabel);
            card.Controls.Add(loginBox);
            card.Controls.Add(passwordLabel);
            card.Controls.Add(passwordBox);
            card.Controls.Add(confirmLabel);
            card.Controls.Add(confirmPasswordBox);
            card.Controls.Add(errorLabel);
            card.Controls.Add(saveButton);

            Controls.Add(card);

            AcceptButton = saveButton;
            ActiveControl = nameBox;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            errorLabel.Text = "";

            try
            {
                Validator.Required(nameBox.Text, "Имя");
                Validator.ValidateLogin(loginBox.Text);
                Validator.ValidatePassword(passwordBox.Text);

                if (passwordBox.Text != confirmPasswordBox.Text)
                {
                    throw new InvalidOperationException("Пароли не совпадают.");
                }

                UserService.Register(
                    nameBox.Text.Trim(),
                    loginBox.Text.Trim(),
                    passwordBox.Text
                );

                NewLogin = loginBox.Text.Trim();

                MessageBox.Show(
                    this,
                    "Регистрация выполнена. Теперь войдите в систему.",
                    "Taskora",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                errorLabel.Text = ex.Message;
            }
        }
    }
}