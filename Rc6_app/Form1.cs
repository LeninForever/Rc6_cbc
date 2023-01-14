using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Rc6_app
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonEncrypt_Click(object sender, EventArgs e)
        {
            if (textBoxSourceText.TextLength == 0)
            {
                MessageBox.Show("Исходный текст не должен быть пустым!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (textBoxKey.TextLength == 0)
            {
                if (MessageBox.Show("Ошибка! Поле \"Ключ\"должно быть не пустым! Установить значение по умолчанию?", "Ошибка", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.OK)
                    textBoxKey.Text = "abcdefghijklmnaa";
                return;
            }
            if (textBoxInitVector.TextLength != 16)
            {
                if (MessageBox.Show("Ошибка! Длина вектора - 16! Ввести значение по умолчанию?", "Ошибка", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.OK)
                    textBoxInitVector.Text = "abcdabcdabcdabcd";
                return;
            }

            string text = textBoxSourceText.Text;
            string initText = textBoxInitVector.Text;
            string textKey = textBoxKey.Text;
            Rc6 rc6 = new Rc6(20, Encoding.UTF8.GetBytes(textKey).Reverse().ToArray(), Encoding.UTF8.GetBytes(initText));

            if ((sender as Button).Name == buttonEncrypt.Name)
            {
                Encrypt(rc6, text);
            }
            else
                Decrypt(rc6, text);
        }

        private void Encrypt(Rc6 rc6, string text)
        {
            var encryptedBytes = rc6.Encrypt(text);
            textBoxEncryptedText.AppendText("\r\n\r\n" + Convert.ToBase64String(encryptedBytes) + "\r\n\r\n");
        }
        private void Decrypt(Rc6 rc6, string text)
        {

            try
            {
                var base64Bytes = Convert.FromBase64String(textBoxSourceText.Text);
                var decryptedStr = rc6.Decrypt(base64Bytes.ToArray());

                int firstIndexZeroSymbol = decryptedStr.IndexOf('\0');
                if (firstIndexZeroSymbol != -1)
                {
                    decryptedStr = decryptedStr.Remove(firstIndexZeroSymbol, decryptedStr.Length - firstIndexZeroSymbol);
                }
                var builder = new StringBuilder();
              
                textBoxEncryptedText.AppendText("\r\n" + decryptedStr + "\r\n");
            }
            catch (Exception)
            {
                var commonBytes = Encoding.UTF8.GetBytes(textBoxSourceText.Text.Trim());
                string decryptedStr = rc6.Decrypt(commonBytes);
                textBoxEncryptedText.AppendText(commonBytes + "\r\n");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBoxEncryptedText.Clear();
            textBoxSourceText.Clear();
        }

        private void buttonLoadDefault_Click(object sender, EventArgs e)
        {
            textBoxKey.Text = "abcdefghijklmnaa";
            textBoxInitVector.Text = "abcdabcdabcdabcd";
        }
    }
}