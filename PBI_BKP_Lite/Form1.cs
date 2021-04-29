using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using MaterialSkin;
using MaterialSkin.Controls;

namespace PBI_BKP_Lite
{
    public partial class frmPBIBKP : MaterialForm
    {
        string PathDestino;
        string usernameProceso;
        string TenantId;
        string IdReporte = "";
        string NombreReporte = "";
        string ArchivoRepo = "";
        int numero = 1;
        string IdDataFlow = "";
        string NombreDataFlow = "";

        public frmPBIBKP()
        {
            InitializeComponent();

            // Cree un administrador de temas de materiales y agregue el formulario para administrar (this)
            MaterialSkinManager materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;

            // Configurar esquema de color
            materialSkinManager.ColorScheme = new ColorScheme(
                 Primary.Green500, Primary.Blue900,
                Primary.Blue900, Accent.LightBlue200,
                TextShade.WHITE
            );

            /*
             * // Configurar esquema de color
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Blue400, Primary.Blue500,
                Primary.Blue500, Accent.LightBlue200,
                TextShade.WHITE
            );*/
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (var fldrDlg = new FolderBrowserDialog())
            {
                if (fldrDlg.ShowDialog() == DialogResult.OK)
                {
                    lblPathDestino.BackColor = Color.Transparent;
                    lblPathDestino.ForeColor = Color.FromArgb(31, 60, 110);

                    materialLabel1.BackColor = Color.Transparent;
                    materialLabel1.ForeColor = Color.FromArgb(31, 60, 110);
                    materialLabel1.Text = "Log:";

                    PathDestino = fldrDlg.SelectedPath;
                    lblPathDestino.Text = "Destino de Respaldos:   " + PathDestino;
                }
                else
                {
                    MessageBox.Show("No se seleccionó Destino de Respaldos", "PBI Backup - Lite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Windows.Forms.Application.Exit();
                }
            }
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            DialogResult boton = MessageBox.Show("¿Está seguro que desea salir?", "PBI Backup - Lite", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
            if (boton == DialogResult.OK)
            {
                System.Windows.Forms.Application.Exit();
            }
        }

        #region Revisa si archivo esta bloqueado

        protected virtual bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }
        #endregion Revisa si archivo esta bloqueado

        #region saca valor de la linea

        protected virtual string Contenido(string Evaluar, int desde)
        {
            return Evaluar.Substring(desde,Evaluar.Length - desde); //Despeja el contenido de la linea, solo lo que es realmente utilizable
        }

        #endregion saca valor de la linea

        protected virtual void EsperaCreacionArchivo(string Archivo)
        {
            while (!File.Exists(Archivo))
            {
                //espera mintras se crea el archivo 
            }
        }

        protected void EsperaDesbloqueoArchivo(string Archivo)
        {
            while (IsFileLocked(new FileInfo(Archivo)))
            {
                //espera mientras se desocupa el archivo 
            }
        }

        protected void BorraArchivo(string Archivo)
        {
            
            #region Elimina Archivos  

            Process cmdRM = new Process();
            cmdRM.StartInfo.FileName = "powershell.exe";
            cmdRM.StartInfo.RedirectStandardInput = true;
            cmdRM.StartInfo.RedirectStandardOutput = true;
            cmdRM.StartInfo.CreateNoWindow = true;
            cmdRM.StartInfo.UseShellExecute = false;
            cmdRM.Start();

            cmdRM.StandardInput.WriteLine(@"rm " + Archivo);

            cmdRM.StandardInput.Flush();
            cmdRM.StandardInput.Close();

            cmdRM.WaitForExit();

            #endregion  Elimina Archivos  
        }

        private void iconButton1_Click(object sender, EventArgs e)
        {
            
            string ArchivoLogin = PathDestino + "\\login.txt";
            string ArchivoWorkspace = PathDestino + "\\Workspaces.txt";
            string ArchivoReportes = PathDestino + "\\Reportes_";
            string ArchivoDF = PathDestino + "\\DF_";

            string[] textLogin;
            Log.Text = "";

            #region Limpia el directorio de salida
            Log.AppendText("Limpiando directorio de salida.");
            Log.AppendText(Environment.NewLine);

            BorraArchivo(PathDestino + "\\*.txt");
            BorraArchivo(PathDestino + "\\*.pbix");
            BorraArchivo(PathDestino + "\\*.json");

            Log.AppendText("Limpieza terminada correctamente.");
            Log.AppendText(Environment.NewLine);

            #endregion Limpia el directorio de salida


            Process cmd = new Process();
            cmd.StartInfo.FileName = "powershell.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.EnableRaisingEvents = true;
            cmd.Start();
            cmd.BeginOutputReadLine();


            #region Login a Power BI

            Log.AppendText("Conectando a PowerBI.");
            Log.AppendText(Environment.NewLine);

            cmd.StandardInput.WriteLine(@"Login-PowerBI > " + ArchivoLogin);

            EsperaCreacionArchivo(ArchivoLogin);
            EsperaDesbloqueoArchivo(ArchivoLogin);

            if (new FileInfo(ArchivoLogin).Length == 0)  //Se crea correctamente la sesion?
            {
                MessageBox.Show("Error en Login a Power BI, por favor intente más tarde", "PBI Backup - Lite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                System.Windows.Forms.Application.Exit();
            }

            textLogin = File.ReadAllLines(ArchivoLogin);

            foreach (string line in textLogin)
            {
                string usernameLogin = "UserName";
                string IdLogin = "Id";
                string separador = ": ";

                if (line.Contains(IdLogin))
                {
                    if (line.Contains(separador))
                    {
                        int index = line.IndexOf(separador);
                        if (index >= 0)
                        {
                            TenantId = Contenido(line, index + separador.Length); // Saca Id Usuario
                        }
                    }
                }

                if (line.Contains(usernameLogin))
                {
                    if (line.Contains(separador))
                    {
                        int index = line.IndexOf(separador);
                        if (index >= 0)
                        {
                            usernameProceso = Contenido(line, index + separador.Length);  // Saca username Usuario
                            MessageBox.Show("Bienvenido: " + usernameProceso);
                        }
                    }
                }
            }
            Log.AppendText("Conectado a PowerBI correctamente.");
            Log.AppendText(Environment.NewLine);

            #endregion  Login a Power BI

            #region Obtiene los Workspaces del usuario

            Log.AppendText("Recuperando Workspaces del usuario " + usernameProceso);
            Log.AppendText(Environment.NewLine);

            cmd.StandardInput.WriteLine(@"Get-PowerBIWorkspace -All > " + ArchivoWorkspace);

            EsperaCreacionArchivo(ArchivoWorkspace);
            EsperaDesbloqueoArchivo(ArchivoWorkspace);

            string [] textWorkspace = File.ReadAllLines(ArchivoWorkspace);

            ArrayList WorkspaceId = new ArrayList();

            foreach (string line in textWorkspace)
            {
                string IdWorkspace = "Id                    :";
                string separador = ": ";

                if (line.Contains(IdWorkspace))
                {
                    if (line.Contains(separador))
                    {
                        int index = line.IndexOf(separador);
                        if (index >= 0)
                        {
                            WorkspaceId.Add(Contenido(line, index + separador.Length));
                        }
                    }
                }
            }

            Log.AppendText("Workspaces recuperados correctamente");
            Log.AppendText(Environment.NewLine);

            #endregion Obtiene los Workspaces del usuario

            #region Obtiene Reportes para backup

            Log.AppendText("Obteniendo Reportes para Backup");
            Log.AppendText(Environment.NewLine);

            foreach (string IdWorkspace in WorkspaceId)
            {
                string ArchivoReportesWorkspace = ArchivoReportes + IdWorkspace +".txt";
                cmd.StandardInput.WriteLine(@"Get-PowerBIReport -WorkspaceId " + IdWorkspace + " > " + ArchivoReportesWorkspace);
                EsperaCreacionArchivo(ArchivoReportesWorkspace);
                EsperaDesbloqueoArchivo(ArchivoReportesWorkspace);

                if (new FileInfo(ArchivoReportesWorkspace).Length > 0)  //Existen Reportes en el Workspace?
                {
                    string[] textReporte = File.ReadAllLines(ArchivoReportesWorkspace);

                    foreach (string line in textReporte)
                    {
                        string IdRep = "Id        :";
                        string Nombre = "Name      :";
                        string separador = ": ";
                        

                        if (line.Contains(IdRep))
                        {
                            int index = line.IndexOf(separador);
                            if (index >= 0)
                            {
                                IdReporte = Contenido(line, index + separador.Length);
                            }
                        }

                        if (line.Contains(Nombre))
                        {
                            int index = line.IndexOf(separador);
                            if (index >= 0)
                            {
                                NombreReporte = Contenido(line, index + separador.Length);
                                NombreReporte = Regex.Replace(NombreReporte.Normalize(NormalizationForm.FormD), @"[^a-zA-Z0-9 ]+", "");
                            }
                        }

                        if ((NombreReporte.Length > 0) && (IdReporte.Length > 0))
                        {
                            Log.AppendText("Recuperando Reporte: " + NombreReporte );
                            Log.AppendText(Environment.NewLine);

                            string Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
                            ArchivoRepo = PathDestino + "\\" + NombreReporte + "_" + Timestamp + numero.ToString() + ".pbix";
                            numero++;
                            cmd.StandardInput.WriteLine(@"Export-PowerBIReport -Id " + IdReporte + " -OutFile \"" + ArchivoRepo + "\" -WorkspaceId " + IdWorkspace);
                            
                            Log.AppendText("Creando archivo backup: " + ArchivoRepo);
                            Log.AppendText(Environment.NewLine);
                            //Console.WriteLine("Creando archivo: " + ArchivoRepo);
                           // EsperaCreacionArchivo(ArchivoRepo);
                           // EsperaDesbloqueoArchivo(ArchivoRepo);
                           
                            IdReporte = NombreReporte = "";
                        }
                        
                    }
                }

                string ArchivoDataFlowWorkspace = ArchivoDF + IdWorkspace + ".txt";

                cmd.StandardInput.WriteLine(@"Get-PowerBIDataflow -WorkspaceId " + IdWorkspace + " > " + ArchivoDataFlowWorkspace);
                EsperaCreacionArchivo(ArchivoDataFlowWorkspace);
                EsperaDesbloqueoArchivo(ArchivoDataFlowWorkspace);

                if (new FileInfo(ArchivoDataFlowWorkspace).Length > 0)  //Existen Reportes en el Workspace?
                {
                    string[] textDF = File.ReadAllLines(ArchivoDataFlowWorkspace);

                    foreach (string line in textDF)
                    {
                        string IdDF = "Id           :";
                        string NombreDF = "Name         :";
                        string separador = ": ";

                        if (line.Contains(IdDF))
                        {
                            int index = line.IndexOf(separador);
                            if (index >= 0)
                            {
                                IdDataFlow = Contenido(line, index + separador.Length);
                            }
                        }

                        if (line.Contains(NombreDF))
                        {
                            int index = line.IndexOf(separador);
                            if (index >= 0)
                            {
                                NombreDataFlow = Contenido(line, index + separador.Length);
                                //NombreDataFlow = Regex.Replace(NombreDataFlow.Normalize(NormalizationForm.FormD), @"[^a-zA-Z0-9 ] + ", "");
                                NombreDataFlow = Regex.Replace(NombreDataFlow.Normalize(NormalizationForm.FormD), @"[^\w]", "");
                            }
                        }

                        if ((NombreDataFlow.Length > 0) && (IdDataFlow.Length > 0))
                        {
                            Log.AppendText("Recuperando Dataflow: " + NombreDataFlow);
                            Log.AppendText(Environment.NewLine);

                            string Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
                            ArchivoDF = PathDestino + "\\" + NombreDataFlow + "_" + Timestamp + numero.ToString() + ".json";
                            numero++;
                            cmd.StandardInput.WriteLine(@"Export-PowerBIDataflow -Id " + IdDataFlow + " -OutFile \"" + ArchivoDF + "\" -WorkspaceId " + IdWorkspace);

                            Log.AppendText("Creando archivo backup: " + ArchivoDF);
                            Log.AppendText(Environment.NewLine);

                            Console.WriteLine("Creando archivo: " + ArchivoDF);
                            // EsperaCreacionArchivo(ArchivoRepo);
                            // EsperaDesbloqueoArchivo(ArchivoRepo);

                            IdDataFlow = NombreDataFlow = "";
                        }
                    }

                }
            }

            Log.AppendText("Backup completado.");
            Log.AppendText(Environment.NewLine);

            #endregion Obtiene Reportes para backup


            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();

            cmd.WaitForExit();

            #region Limpia archivos Temporales
            Log.AppendText("Borrando Temporales");
            Log.AppendText(Environment.NewLine);

            BorraArchivo(PathDestino + "\\*.txt");

            Log.AppendText("Temporales Borrados correctamente.");
            Log.AppendText(Environment.NewLine);

            #endregion Limpia archivos Temporales

            MessageBox.Show("Proceso ha terminado correctamente. Desarrollado por iNegocios(R) 2021");
            Log.AppendText("Fin exitoso del proceso.");
            Log.AppendText(Environment.NewLine);

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
