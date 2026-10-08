using System;
using System.Drawing;
using System.Windows.Forms;
using Taskora.БазаДанных;
using Taskora.Логика;
using Taskora.Модели;

namespace Taskora.Формы
{
    public class TaskForm : Form
    {
        private readonly int _taskId;

        private TextBox titleBox;
        private TextBox descriptionBox;
        private ComboBox categoryBox;
        private ComboBox priorityBox;
        private ComboBox statusBox;
        private CheckBox dueCheckBox;
        private DateTimePicker duePicker;
        private Label errorLabel;

        private bool IsEdit => _taskId > 0;

        public TaskForm(int taskId)
        {
            _taskId = taskId;
            InitializeUi();
            LoadCategories();

            if (IsEdit)
                LoadTask();
        }

        private void InitializeUi()
        {
            Icon = Ui.LoadIcon();
            Text = IsEdit ? "Taskora — Редактирование задачи" : "Taskora — Новая задача";
            ClientSize = new Size(560, 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Ui.Background;
            Font = Ui.GetFont(10);

            RoundedPanel card = new RoundedPanel
            {
                Bounds = new Rectangle(20, 20, 520, 680)
            };

            int y = 20;

            AddLabel(card, "Название задачи *", ref y);

            titleBox = Ui.CreateTextBox(460);
            titleBox.Location = new Point(24, y);
            card.Controls.Add(titleBox);
            y += 45;

            AddLabel(card, "Описание", ref y);

            descriptionBox = new TextBox
            {
                Location = new Point(24, y),
                Size = new Size(460, 90),
                Multiline = true,
                Font = Ui.GetFont(10)
            };
            card.Controls.Add(descriptionBox);
            y += 105;

            AddLabel(card, "Категория", ref y);

            categoryBox = Ui.CreateCombo(460, true);
            categoryBox.Location = new Point(24, y);
            card.Controls.Add(categoryBox);
            y += 45;

            AddLabel(card, "Приоритет", ref y);

            priorityBox = Ui.CreateCombo(460);
            priorityBox.Location = new Point(24, y);
            priorityBox.Items.AddRange(TaskService.Priorities);
            priorityBox.SelectedItem = "Средний";
            card.Controls.Add(priorityBox);
            y += 45;

            AddLabel(card, "Статус", ref y);

            statusBox = Ui.CreateCombo(460);
            statusBox.Location = new Point(24, y);
            statusBox.Items.AddRange(TaskService.Statuses);
            statusBox.SelectedItem = "Новая";
            card.Controls.Add(statusBox);
            y += 45;

            dueCheckBox = new CheckBox
            {
                Text = "Установить срок выполнения",
                Location = new Point(24, y),
                AutoSize = true,
                Font = Ui.GetFont(10),
                ForeColor = Ui.Text
            };
            dueCheckBox.CheckedChanged += (s, e) => duePicker.Enabled = dueCheckBox.Checked;
            card.Controls.Add(dueCheckBox);
            y += 35;

            duePicker = new DateTimePicker
            {
                Location = new Point(24, y),
                Size = new Size(460, 32),
                Font = Ui.GetFont(10),
                Enabled = false,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            card.Controls.Add(duePicker);
            y += 60;

            errorLabel = new Label
            {
                Bounds = new Rectangle(24, y, 460, 50),
                Font = Ui.GetFont(9),
                ForeColor = Ui.Danger
            };
            card.Controls.Add(errorLabel);
            y += 55;

            Button saveButton = Ui.CreateButton(
                IsEdit ? "Сохранить изменения" : "Создать задачу",
                Ui.Primary,
                new Size(460, 46)
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

        private void LoadCategories()
        {
            categoryBox.Items.Clear();

            string[] presets = { "Работа", "Учёба", "Дом", "Личное", "Финансы" };

            foreach (string preset in presets)
            {
                if (!categoryBox.Items.Contains(preset))
                    categoryBox.Items.Add(preset);
            }

            foreach (string category in TaskService.GetCategories(Session.CurrentUser.Id, Session.IsAdmin))
            {
                if (!categoryBox.Items.Contains(category))
                    categoryBox.Items.Add(category);
            }
        }

        private void LoadTask()
        {
            TaskItem task = TaskService.GetById(_taskId);

            if (task == null)
            {
                MessageBox.Show(this, "Задача не найдена.", "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            titleBox.Text = task.Название;
            descriptionBox.Text = task.Описание;
            categoryBox.Text = task.Категория;
            priorityBox.SelectedItem = task.Приоритет;
            statusBox.SelectedItem = task.Статус;

            if (task.Срок.HasValue)
            {
                dueCheckBox.Checked = true;
                duePicker.Value = task.Срок.Value;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            errorLabel.Text = "";

            try
            {
                Validator.Required(titleBox.Text, "Название задачи");

                TaskItem task = new TaskItem
                {
                    Id = _taskId,
                    Название = titleBox.Text.Trim(),
                    Описание = descriptionBox.Text.Trim(),
                    Категория = string.IsNullOrWhiteSpace(categoryBox.Text)
                        ? "Без категории"
                        : categoryBox.Text.Trim(),
                    Приоритет = priorityBox.SelectedItem?.ToString() ?? "Средний",
                    Статус = statusBox.SelectedItem?.ToString() ?? "Новая",
                    Срок = dueCheckBox.Checked ? duePicker.Value.Date : null
                };

                if (IsEdit)
                {
                    TaskService.Update(task, Session.CurrentUser.Id, Session.IsAdmin);
                }
                else
                {
                    TaskService.Create(task, Session.CurrentUser.Id);
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