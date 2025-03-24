namespace banbotspam
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            button1 = new Button();
            button2 = new Button();
            richTextBox1 = new RichTextBox();
            button4 = new Button();
            textBox1 = new TextBox();
            label1 = new Label();
            textBox2 = new TextBox();
            label2 = new Label();
            checkBox2 = new CheckBox();
            textBox3 = new TextBox();
            label3 = new Label();
            label4 = new Label();
            textBox4 = new TextBox();
            label5 = new Label();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Font = new Font("Leelawadee UI", 27.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button1.Location = new Point(44, 168);
            button1.Name = "button1";
            button1.Size = new Size(215, 102);
            button1.TabIndex = 0;
            button1.Text = "แบน!!";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Font = new Font("Leelawadee UI", 27.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button2.Location = new Point(44, 36);
            button2.Name = "button2";
            button2.Size = new Size(215, 102);
            button2.TabIndex = 1;
            button2.Text = "ดูด";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(310, 12);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(1046, 621);
            richTextBox1.TabIndex = 2;
            richTextBox1.Text = "";
            // 
            // button4
            // 
            button4.Font = new Font("Leelawadee UI", 27.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button4.Location = new Point(44, 308);
            button4.Name = "button4";
            button4.Size = new Size(215, 102);
            button4.TabIndex = 4;
            button4.Text = "เลือกsecret";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(44, 437);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(215, 23);
            textBox1.TabIndex = 5;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(44, 419);
            label1.Name = "label1";
            label1.Size = new Size(69, 15);
            label1.TabIndex = 8;
            label1.Text = "Channel Url";
            label1.Click += label1_Click;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(35, 600);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(215, 23);
            textBox2.TabIndex = 9;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(35, 582);
            label2.Name = "label2";
            label2.Size = new Size(177, 15);
            label2.TabIndex = 10;
            label2.Text = "Yotube Url (ถ้าต้องการเช็คตามคลิป)";
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Location = new Point(35, 550);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(50, 19);
            checkBox2.TabIndex = 11;
            checkBox2.Text = "loop";
            checkBox2.UseVisualStyleBackColor = true;
            checkBox2.CheckedChanged += checkBox2_CheckedChanged;
            // 
            // textBox3
            // 
            textBox3.Location = new Point(91, 548);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(32, 23);
            textBox3.TabIndex = 12;
            textBox3.TextChanged += textBox3_TextChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Leelawadee UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.Location = new Point(129, 539);
            label3.Name = "label3";
            label3.Size = new Size(53, 32);
            label3.TabIndex = 13;
            label3.Text = "นาที";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(44, 474);
            label4.Name = "label4";
            label4.Size = new Size(239, 15);
            label4.TabIndex = 15;
            label4.Text = "จำนวนคลิปที่ต้องการเช็ค (นับจากคลิปอับโหลดล่าสุด)";
            // 
            // textBox4
            // 
            textBox4.Location = new Point(44, 492);
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(29, 23);
            textBox4.TabIndex = 14;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(86, 500);
            label5.Name = "label5";
            label5.Size = new Size(27, 15);
            label5.TabIndex = 16;
            label5.Text = "คลิป";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1368, 635);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(textBox4);
            Controls.Add(label3);
            Controls.Add(textBox3);
            Controls.Add(checkBox2);
            Controls.Add(label2);
            Controls.Add(textBox2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(button4);
            Controls.Add(richTextBox1);
            Controls.Add(button2);
            Controls.Add(button1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "banbot BY Cattales";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Button button2;
        private RichTextBox richTextBox1;
        private Button button4;
        private TextBox textBox1;
        private Label label1;
        private TextBox textBox2;
        private Label label2;
        private CheckBox checkBox2;
        private TextBox textBox3;
        private Label label3;
        private Label label4;
        private TextBox textBox4;
        private Label label5;
    }
}
