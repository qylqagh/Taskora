using System;
using System.Drawing;
using System.Windows.Forms;
using Taskora.БазаДанных;
using Taskora.Логика;
using Taskora.Модели;

namespace Taskora.Формы
{
    public class UserForm : Form
    {
        private readonly int _userId;

        private TextBox nameBox;
        private TextBox loginBox;
        private TextBox passwordBox;
        private ComboBox roleBox;
        private Label errorLabel;

        private bool IsEdit => _userId > 0;

        public UserForm(int userId)
        {
            _userId = userId;
            InitializeUi();

            if (IsEdit)
                LoadUser();
        }

        private void InitializeUi()
        {
            Icon = Ui.LoadIcon();
            Text = IsEdit ? "Taskora — Редактирование пользователя" : "Taskora — Новый пользователь";
            ClientSize = new Size(470, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Ui.Background;
            Font = Ui.GetFont(10);

            RoundedPanel card = new RoundedPanel
            {
                Bounds = new Rectangle(20, 20, 430, 520)
            };

            int y = 20;

            AddLabel(card, "Имя *", ref y);

            nameBox = Ui.CreateTextBox(380);
            nameBox.Location = new Point(24, y);
            card.Controls.Add(nameBox);
            y += 45;

            AddLabel(card, "Логин *", ref y);

            loginBox = Ui.CreateTextBox(380);
            loginBox.Location = new Point(24, y);
            card.Controls.Add(loginBox);
            y += 45;

            AddLabel(card, IsEdit ? "Новый пароль" : "Пароль *", ref y);

            passwordBox = Ui.CreateTextBox(380);
            passwordBox.Location = new Point(24, y);
            passwordBox.PasswordChar = '•';
            passwordBox.UseSystemPasswordChar = true;

            if (IsEdit)
                passwordBox.PlaceholderText = "Оставьте пустым, чтобы не менять пароль";

            card.Controls.Add(passwordBox);
            y += 45;

            AddLabel(card, "Роль", ref y);

            roleBox = Ui.CreateCombo(380);
            roleBox.Location = new Point(24, y);
            roleBox.Items.Add(UserService.RoleUser);
            roleBox.Items.Add(UserService.RoleAdmin);
            roleBox.SelectedIndex = 0;
            card.Controls.Add(roleBox);
            y += 55;

            errorLabel = new Label
            {
                Bounds = new Rectangle(24, y, 380, 70),
                Font = Ui.GetFont(9),
                ForeColor = Ui.Danger
            };
            card.Controls.Add(errorLabel);
            y += 75;

            Button saveButton = Ui.CreateButton(
                IsEdit ? "Сохранить изменения" : "Создать пользователя",
                Ui.Primary,
                new Size(380, 46)
            );

            saveButton.Location = new Point(24, y);
            saveButton.Click += SaveButton_Click;

            card.Controls.Add(saveButton);

            Controls.Add(card);

            AcceptButton = saveButton;
        }

        private void AddLabel(Control parent, string text, ref int y)
        {
            Label label = Ui.CreateLabel(text, 10, FontStyle.Bold, Ui.Text);
            label.Location = new Point(24, y);
            parent.Controls.Add(label);
            y += 26;
        }

        private void LoadUser()
        {
            User user = UserService.GetById(_userId);

            if (user == null)
            {
                MessageBox.Show(this, "Пользователь не найден.", "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            nameBox.Text = user.Имя;
            loginBox.Text = user.Логин;
            roleBox.SelectedItem = user.Роль;

            if (Session.CurrentUser != null && Session.CurrentUser.Id == _userId)
            {
                roleBox.Enabled = false;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            errorLabel.Text = "";

            try
            {
                Validator.Required(nameBox.Text, "Имя");
                Validator.ValidateLogin(loginBox.Text);

                string role = roleBox.SelectedItem?.ToString() ?? UserService.RoleUser;

                if (!IsEdit)
                {
                    Validator.ValidatePassword(passwordBox.Text);

                    UserService.Create(
                        nameBox.Text.Trim(),
                        loginBox.Text.Trim(),
                        passwordBox.Text,
                        role
                    );
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(passwordBox.Text))
                        Validator.ValidatePassword(passwordBox.Text);

                    UserService.Update(
                        _userId,
                        nameBox.Text.Trim(),
                        loginBox.Text.Trim(),
                        passwordBox.Text.Trim(),
                        role,
                        Session.CurrentUser.Id
                    );
                }

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