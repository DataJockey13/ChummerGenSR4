﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

// ServicesOwedChanged Event Handler.
public delegate void ServicesOwedChangedHandler(Object sender);
// ForceChanged Event Handler.
public delegate void ForceChangedHandler(Object sender);
// BoundChanged Event Handler.
public delegate void BoundChangedHandler(Object sender);
// DeleteSpirit Event Handler.
public delegate void DeleteSpiritHandler(Object sender);

namespace Chummer
{
    public partial class SpiritControl : UserControl
    {
		private Spirit _objSpirit;
		private readonly bool _blnCareer = false;

        // Events.
        public event ServicesOwedChangedHandler ServicesOwedChanged;
		public event ForceChangedHandler ForceChanged;
		public event BoundChangedHandler BoundChanged;
        public event DeleteSpiritHandler DeleteSpirit;
		public event FileNameChangedHandler FileNameChanged;

		#region Control Events
		public SpiritControl(bool blnCareer = false)
        {
            InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			_blnCareer = blnCareer;
			chkBound.Enabled = blnCareer;
        }

		private void nudServices_ValueChanged(object sender, EventArgs e)
        {
            // Raise the ServicesOwedChanged Event when the NumericUpDown's Value changes.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
			_objSpirit.ServicesOwed = Convert.ToInt32(nudServices.Value);
            ServicesOwedChanged(this);
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            // Raise the DeleteSpirit Event when the user has confirmed their desire to delete the Spirit.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
			DeleteSpirit(this);
        }

		private void nudForce_ValueChanged(object sender, EventArgs e)
		{
			// Raise the ForceChanged Event when the NumericUpDown's Value changes.
			// The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
			_objSpirit.Force = Convert.ToInt32(nudForce.Value);
			ForceChanged(this);
		}

		private void chkBound_CheckedChanged(object sender, EventArgs e)
		{
			// Raise the BoundChanged Event when the Checkbox's Checked status changes.
			// The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
			_objSpirit.Bound = chkBound.Checked;
			BoundChanged(this);
		}

		private void SpiritControl_Load(object sender, EventArgs e)
		{
			if (_blnCareer)
				nudForce.Enabled = true;
			this.Width = cmdDelete.Left + cmdDelete.Width;
		}

		private void cboSpiritName_TextChanged(object sender, EventArgs e)
		{
			_objSpirit.Name = cboSpiritName.Text;
		}

		private void cboSpiritName_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboSpiritName.SelectedValue != null)
				_objSpirit.Name = cboSpiritName.SelectedValue.ToString();
			ForceChanged(this);
		}

		private void txtCritterName_TextChanged(object sender, EventArgs e)
		{
			_objSpirit.CritterName = txtCritterName.Text;
			ForceChanged(this);
		}

		private void tsContactOpen_Click(object sender, EventArgs e)
		{
			bool blnError = false;
			bool blnUseRelative = false;

			// Make sure the file still exists before attempting to load it.
			if (!File.Exists(_objSpirit.FileName))
			{
				// If the file doesn't exist, use the relative path if one is available.
				if (_objSpirit.RelativeFileName == "")
					blnError = true;
				else
				{
					MessageBox.Show(Path.GetFullPath(_objSpirit.RelativeFileName));
					if (!File.Exists(Path.GetFullPath(_objSpirit.RelativeFileName)))
						blnError = true;
					else
						blnUseRelative = true;
				}

				if (blnError)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_FileNotFound").Replace("{0}", _objSpirit.FileName), LanguageManager.Instance.GetString("MessageTitle_FileNotFound"), MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			if (!blnUseRelative)
				GlobalOptions.Instance.MainForm.LoadCharacter(_objSpirit.FileName, false);
			else
			{
				string strFile = Path.GetFullPath(_objSpirit.RelativeFileName);
				GlobalOptions.Instance.MainForm.LoadCharacter(strFile, false);
			}
		}

		private void tsRemoveCharacter_Click(object sender, EventArgs e)
		{
			// Remove the file association from the Contact.
			if (MessageBox.Show(LanguageManager.Instance.GetString("Message_RemoveCharacterAssociation"), LanguageManager.Instance.GetString("MessageTitle_RemoveCharacterAssociation"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				_objSpirit.FileName = "";
				_objSpirit.RelativeFileName = "";
				if (_objSpirit.EntityType ==  SpiritType.Spirit)
					tipTooltip.SetToolTip(imgLink, LanguageManager.Instance.GetString("Tip_Spirit_LinkSpirit"));
				else
					tipTooltip.SetToolTip(imgLink, LanguageManager.Instance.GetString("Tip_Sprite_LinkSprite"));

				// Set the relative path.
				Uri uriApplication = new Uri(@Application.StartupPath);
				Uri uriFile = new Uri(@_objSpirit.FileName);
				Uri uriRelative = uriApplication.MakeRelativeUri(uriFile);
				_objSpirit.RelativeFileName = "../" + uriRelative.ToString();

				FileNameChanged(this);
			}
		}

		private void tsAttachCharacter_Click(object sender, EventArgs e)
		{
			// Prompt the user to select a save file to associate with this Contact.
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Chummer Files (*.chum)|*.chum|All Files (*.*)|*.*";

			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				_objSpirit.FileName = openFileDialog.FileName;
				if (_objSpirit.EntityType == SpiritType.Spirit)
					tipTooltip.SetToolTip(imgLink, LanguageManager.Instance.GetString("Tip_Spirit_OpenFile"));
				else
					tipTooltip.SetToolTip(imgLink, LanguageManager.Instance.GetString("Tip_Sprite_OpenFile"));
				FileNameChanged(this);
			}
		}

		private void tsCreateCharacter_Click(object sender, EventArgs e)
		{
			if (cboSpiritName.Text == string.Empty)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectCritterType"), LanguageManager.Instance.GetString("MessageTitle_SelectCritterType"), MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			CreateCritter(cboSpiritName.SelectedValue.ToString(), Convert.ToInt32(nudForce.Value));
		}

		private void imgLink_Click(object sender, EventArgs e)
		{
			// Determine which options should be shown based on the FileName value.
			if (_objSpirit.FileName != "")
			{
				tsAttachCharacter.Visible = false;
				tsCreateCharacter.Visible = false;
				tsContactOpen.Visible = true;
				tsRemoveCharacter.Visible = true;
			}
			else
			{
				tsAttachCharacter.Visible = true;
				tsCreateCharacter.Visible = true;
				tsContactOpen.Visible = false;
				tsRemoveCharacter.Visible = false;
			}
			cmsSpirit.Show(imgLink, imgLink.Left - 646, imgLink.Top);
		}

		private void imgNotes_Click(object sender, EventArgs e)
		{
			frmNotes frmSpritNotes = new frmNotes();
			frmSpritNotes.Notes = _objSpirit.Notes;
			frmSpritNotes.ShowDialog(this);

			if (frmSpritNotes.DialogResult == DialogResult.OK)
				_objSpirit.Notes = frmSpritNotes.Notes;

			string strTooltip = "";
			if (_objSpirit.EntityType == SpiritType.Spirit)
				strTooltip = LanguageManager.Instance.GetString("Tip_Spirit_EditNotes");
			else
				strTooltip = LanguageManager.Instance.GetString("Tip_Sprite_EditNotes");
			if (_objSpirit.Notes != string.Empty)
				strTooltip += "\n\n" + _objSpirit.Notes;
			tipTooltip.SetToolTip(imgNotes, strTooltip);
		}

		private void ContextMenu_Opening(object sender, CancelEventArgs e)
		{
			foreach (ToolStripItem objItem in ((ContextMenuStrip)sender).Items)
			{
				if (objItem.Tag != null)
				{
					objItem.Text = LanguageManager.Instance.GetString(objItem.Tag.ToString());
				}
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Spirit object this is linked to.
		/// </summary>
		public Spirit SpiritObject
		{
			get
			{
				return _objSpirit;
			}
			set
			{
				_objSpirit = value;
			}
		}

        /// <summary>
        /// Spirit Metatype name.
        /// </summary>
        public string SpiritName
        {
            get
            {
				return _objSpirit.Name;
            }
            set
            {
				cboSpiritName.Text = value;
				_objSpirit.Name = value;
            }
        }

		/// <summary>
		/// Spirit name.
		/// </summary>
		public string CritterName
		{
			get
			{
				return _objSpirit.CritterName;
			}
			set
			{
				txtCritterName.Text = value;
				_objSpirit.CritterName = value;
			}
		}

        /// <summary>
        /// Indicates if this is a Spirit or Sprite. For labeling purposes only.
        /// </summary>
        public SpiritType EntityType
        {
            get
            {
				return _objSpirit.EntityType;
            }
            set
            {
				_objSpirit.EntityType = value;
				if (value == SpiritType.Spirit)
				{
					lblForce.Text = LanguageManager.Instance.GetString("Label_Spirit_Force");
					chkBound.Text = LanguageManager.Instance.GetString("Checkbox_Spirit_Bound");
					if (_objSpirit.FileName != "")
						tipTooltip.SetToolTip(imgLink, LanguageManager.Instance.GetString("Tip_Spirit_OpenFile"));
					else
						tipTooltip.SetToolTip(imgLink, LanguageManager.Instance.GetString("Tip_Spirit_LinkSpirit"));

					string strTooltip = LanguageManager.Instance.GetString("Tip_Spirit_EditNotes");
					if (_objSpirit.Notes != string.Empty)
						strTooltip += "\n\n" + _objSpirit.Notes;
					tipTooltip.SetToolTip(imgNotes, strTooltip);
				}
				else
				{
					lblForce.Text = LanguageManager.Instance.GetString("Label_Sprite_Rating");
					chkBound.Text = LanguageManager.Instance.GetString("Label_Sprite_Registered");
					if (_objSpirit.FileName != "")
						tipTooltip.SetToolTip(imgLink, "Open the linked Sprite save file.");
					else
						tipTooltip.SetToolTip(imgLink, "Link this Sprite to a Chummer save file.");

					string strTooltip = LanguageManager.Instance.GetString("Tip_Sprite_EditNotes");
					if (_objSpirit.Notes != string.Empty)
						strTooltip += "\n\n" + _objSpirit.Notes;
					tipTooltip.SetToolTip(imgNotes, strTooltip);
				}
            }
        }

        /// <summary>
        /// Services owed.
        /// </summary>
        public int ServicesOwed
        {
            get
            {
				return _objSpirit.ServicesOwed;
            }
            set
            {
				nudServices.Value = value;
				_objSpirit.ServicesOwed = value;
            }
        }

		/// <summary>
		/// Force of the Spirit.
		/// </summary>
		public int Force
		{
			get
			{
				return _objSpirit.Force;
			}
			set
			{
				nudForce.Value = value;
				_objSpirit.Force = value;
			}
		}

		/// <summary>
		/// Maximum Force of the Spirit.
		/// </summary>
		public int ForceMaximum
		{
			get
			{
				return Convert.ToInt32(nudForce.Maximum);
			}
			set
			{
				nudForce.Maximum = value;
			}
		}

		/// <summary>
		/// Whether or not the Spirit is Bound.
		/// </summary>
		public bool Bound
		{
			get
			{
				return _objSpirit.Bound;
			}
			set
			{
				chkBound.Checked = value;
				_objSpirit.Bound = value;
			}
		}
		#endregion

		#region Methods
		// Rebuild the list of Spirits/Sprites based on the character's selected Tradition/Stream.
		public void RebuildSpiritList(string strTradition)
		{
			string strCurrentValue = "";
			if (cboSpiritName.SelectedValue != null)
				strCurrentValue = cboSpiritName.SelectedValue.ToString();
			else
				strCurrentValue = _objSpirit.Name;

			XmlDocument objXmlDocument = new XmlDocument();
			XmlDocument objXmlCritterDocument = new XmlDocument();
			if (_objSpirit.EntityType == SpiritType.Spirit)
				objXmlDocument = XmlManager.Instance.Load("traditions.xml");
			else
				objXmlDocument = XmlManager.Instance.Load("streams.xml");
			objXmlCritterDocument = XmlManager.Instance.Load("critters.xml");

			List<ListItem> lstCritters = new List<ListItem>();
			foreach (XmlNode objXmlSpirit in objXmlDocument.SelectNodes("/chummer/traditions/tradition[name = \"" + strTradition + "\"]/spirits/spirit"))
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlSpirit.InnerText;
				XmlNode objXmlCritterNode = objXmlCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlSpirit.InnerText + "\"]");
				if (objXmlCritterNode["translate"] != null)
					objItem.Name = objXmlCritterNode["translate"].InnerText;
				else
					objItem.Name = objXmlSpirit.InnerText;

				lstCritters.Add(objItem);
			}

			if (_objSpirit.CharacterObject.RESEnabled)
			{
				// Add any additional Sprites the character has Access to through Sprite Link.
				foreach (Improvement objImprovement in _objSpirit.CharacterObject.Improvements)
				{
					if (objImprovement.ImproveType == Improvement.ImprovementType.AddSprite)
					{
						ListItem objItem = new ListItem();
						objItem.Value = objImprovement.ImprovedName;
						objItem.Name = objImprovement.ImprovedName;
						lstCritters.Add(objItem);
					}
				}
			}

			cboSpiritName.DisplayMember = "Name";
			cboSpiritName.ValueMember = "Value";
			cboSpiritName.DataSource = lstCritters;

			// Set the control back to its original value.
			cboSpiritName.SelectedValue = strCurrentValue;
		}

		/// <summary>
		/// Create a Critter, put them into Career Mode, link them, and open the newly-created Critter.
		/// </summary>
		/// <param name="strCritterName">Name of the Critter's Metatype.</param>
		/// <param name="intForce">Critter's Force.</param>
		private void CreateCritter(string strCritterName, int intForce)
		{
			// The Critter should use the same settings file as the character.
			Character objCharacter = new Character();
			objCharacter.SettingsFile = _objSpirit.CharacterObject.SettingsFile;

			// Override the defaults for the setting.
			objCharacter.IgnoreRules = true;
			objCharacter.IsCritter = true;
			objCharacter.BuildMethod = CharacterBuildMethod.BP;
			objCharacter.BuildPoints = 0;

			if (txtCritterName.Text != string.Empty)
				objCharacter.Name = txtCritterName.Text;

			// Make sure that Running Wild is one of the allowed source books since most of the Critter Powers come from this book.
			bool blnRunningWild = false;
			blnRunningWild = (objCharacter.Options.Books.Contains("RW"));

			if (!blnRunningWild)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_Main_RunningWild"), LanguageManager.Instance.GetString("MessageTitle_Main_RunningWild"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Ask the user to select a filename for the new character.
			string strForce = LanguageManager.Instance.GetString("String_Force");
			if (_objSpirit.EntityType == SpiritType.Sprite)
				strForce = LanguageManager.Instance.GetString("String_Rating");
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Chummer Files (*.chum)|*.chum|All Files (*.*)|*.*";
			saveFileDialog.FileName = strCritterName + " (" + strForce + " " + _objSpirit.Force.ToString() + ").chum";
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				string strFileName = saveFileDialog.FileName;
				objCharacter.FileName = strFileName;
			}
			else
				return;

			// Code from frmMetatype.
			ImprovementManager objImprovementManager = new ImprovementManager(objCharacter);
			XmlDocument objXmlDocument = XmlManager.Instance.Load("critters.xml");

			XmlNode objXmlMetatype = objXmlDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + strCritterName + "\"]");

			// If the Critter could not be found, show an error and get out of here.
			if (objXmlMetatype == null)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_UnknownCritterType").Replace("{0}", strCritterName), LanguageManager.Instance.GetString("MessageTitle_SelectCritterType"), MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Set Metatype information.
			if (strCritterName == "Ally Spirit")
			{
				objCharacter.BOD.AssignLimits(ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["bodmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["bodaug"].InnerText, intForce, 0));
				objCharacter.AGI.AssignLimits(ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["agimax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["agiaug"].InnerText, intForce, 0));
				objCharacter.REA.AssignLimits(ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["reamax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["reaaug"].InnerText, intForce, 0));
				objCharacter.STR.AssignLimits(ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["strmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["straug"].InnerText, intForce, 0));
				objCharacter.CHA.AssignLimits(ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["chamax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["chaaug"].InnerText, intForce, 0));
				objCharacter.INT.AssignLimits(ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["intmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["intaug"].InnerText, intForce, 0));
				objCharacter.LOG.AssignLimits(ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["logmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["logaug"].InnerText, intForce, 0));
				objCharacter.WIL.AssignLimits(ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["wilmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["wilaug"].InnerText, intForce, 0));
				objCharacter.INI.AssignLimits(ExpressionToString(objXmlMetatype["inimin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["inimax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["iniaug"].InnerText, intForce, 0));
				objCharacter.MAG.AssignLimits(ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["magmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["magaug"].InnerText, intForce, 0));
				objCharacter.RES.AssignLimits(ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["resmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["resaug"].InnerText, intForce, 0));
				objCharacter.EDG.AssignLimits(ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["edgmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["edgaug"].InnerText, intForce, 0));
				objCharacter.ESS.AssignLimits(ExpressionToString(objXmlMetatype["essmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essaug"].InnerText, intForce, 0));
			}
			else
			{
				int intMinModifier = -3;
				if (objXmlMetatype["category"].InnerText == "Mutant Critters")
					intMinModifier = 0;
				objCharacter.BOD.AssignLimits(ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 3));
				objCharacter.AGI.AssignLimits(ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 3));
				objCharacter.REA.AssignLimits(ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 3));
				objCharacter.STR.AssignLimits(ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 3));
				objCharacter.CHA.AssignLimits(ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 3));
				objCharacter.INT.AssignLimits(ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 3));
				objCharacter.LOG.AssignLimits(ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 3));
				objCharacter.WIL.AssignLimits(ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 3));
				objCharacter.INI.AssignLimits(ExpressionToString(objXmlMetatype["inimin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["inimax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["iniaug"].InnerText, intForce, 0));
				objCharacter.MAG.AssignLimits(ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 3));
				objCharacter.RES.AssignLimits(ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 3));
				objCharacter.EDG.AssignLimits(ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 3));
				objCharacter.ESS.AssignLimits(ExpressionToString(objXmlMetatype["essmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essaug"].InnerText, intForce, 0));
			}

			// If we're working with a Critter, set the Attributes to their default values.
			objCharacter.BOD.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 0));
			objCharacter.AGI.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 0));
			objCharacter.REA.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 0));
			objCharacter.STR.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 0));
			objCharacter.CHA.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 0));
			objCharacter.INT.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 0));
			objCharacter.LOG.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 0));
			objCharacter.WIL.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 0));
			objCharacter.MAG.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 0));
			objCharacter.RES.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 0));
			objCharacter.EDG.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 0));
			objCharacter.ESS.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["essmax"].InnerText, intForce, 0));

			// Sprites can never have Physical Attributes or WIL.
			if (objXmlMetatype["category"].InnerText.EndsWith("Sprite"))
			{
				objCharacter.BOD.AssignLimits("0", "0", "0");
				objCharacter.AGI.AssignLimits("0", "0", "0");
				objCharacter.REA.AssignLimits("0", "0", "0");
				objCharacter.STR.AssignLimits("0", "0", "0");
				objCharacter.WIL.AssignLimits("0", "0", "0");
				objCharacter.INI.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["inimax"].InnerText, intForce, 0));
				objCharacter.INI.MetatypeMaximum = Convert.ToInt32(ExpressionToString(objXmlMetatype["inimax"].InnerText, intForce, 0));
			}

			objCharacter.Metatype = strCritterName;
			objCharacter.MetatypeCategory = objXmlMetatype["category"].InnerText;
			objCharacter.Metavariant = "";
			objCharacter.MetatypeBP = 0;

			if (objXmlMetatype["movement"] != null)
				objCharacter.Movement = objXmlMetatype["movement"].InnerText;
			// Load the Qualities file.
			XmlDocument objXmlQualityDocument = XmlManager.Instance.Load("qualities.xml");

			// Determine if the Metatype has any bonuses.
			if (objXmlMetatype.InnerXml.Contains("bonus"))
				objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Metatype, strCritterName, objXmlMetatype.SelectSingleNode("bonus"), false, 1, strCritterName);

			// Create the Qualities that come with the Metatype.
			foreach (XmlNode objXmlQualityItem in objXmlMetatype.SelectNodes("qualities/positive/quality"))
			{
				XmlNode objXmlQuality = objXmlQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQualityItem.InnerText + "\"]");
				TreeNode objNode = new TreeNode();
				List<Weapon> objWeapons = new List<Weapon>();
				List<TreeNode> objWeaponNodes = new List<TreeNode>();
				Quality objQuality = new Quality(objCharacter);
				string strForceValue = "";
				if (objXmlQualityItem.Attributes["select"] != null)
					strForceValue = objXmlQualityItem.Attributes["select"].InnerText;
				QualitySource objSource = new QualitySource();
				objSource = QualitySource.Metatype;
				if (objXmlQualityItem.Attributes["removable"] != null)
					objSource = QualitySource.MetatypeRemovable;
				objQuality.Create(objXmlQuality, objCharacter, objSource, objNode, objWeapons, objWeaponNodes, strForceValue);
				objCharacter.Qualities.Add(objQuality);

				// Add any created Weapons to the character.
				foreach (Weapon objWeapon in objWeapons)
					objCharacter.Weapons.Add(objWeapon);
			}
			foreach (XmlNode objXmlQualityItem in objXmlMetatype.SelectNodes("qualities/negative/quality"))
			{
				XmlNode objXmlQuality = objXmlQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQualityItem.InnerText + "\"]");
				TreeNode objNode = new TreeNode();
				List<Weapon> objWeapons = new List<Weapon>();
				List<TreeNode> objWeaponNodes = new List<TreeNode>();
				Quality objQuality = new Quality(objCharacter);
				string strForceValue = "";
				if (objXmlQualityItem.Attributes["select"] != null)
					strForceValue = objXmlQualityItem.Attributes["select"].InnerText;
				QualitySource objSource = new QualitySource();
				objSource = QualitySource.Metatype;
				if (objXmlQualityItem.Attributes["removable"] != null)
					objSource = QualitySource.MetatypeRemovable;
				objQuality.Create(objXmlQuality, objCharacter, objSource, objNode, objWeapons, objWeaponNodes, strForceValue);
				objCharacter.Qualities.Add(objQuality);

				// Add any created Weapons to the character.
				foreach (Weapon objWeapon in objWeapons)
					objCharacter.Weapons.Add(objWeapon);
			}

			// Add any Critter Powers the Metatype/Critter should have.
			XmlNode objXmlCritter = objXmlDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objCharacter.Metatype + "\"]");

			objXmlDocument = XmlManager.Instance.Load("critterpowers.xml");
			foreach (XmlNode objXmlPower in objXmlCritter.SelectNodes("powers/power"))
			{
				XmlNode objXmlCritterPower = objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + objXmlPower.InnerText + "\"]");
				TreeNode objNode = new TreeNode();
				CritterPower objPower = new CritterPower(objCharacter);
				string strForcedValue = "";
				int intRating = 0;

				if (objXmlPower.Attributes["rating"] != null)
					intRating = Convert.ToInt32(objXmlPower.Attributes["rating"].InnerText);
				if (objXmlPower.Attributes["select"] != null)
					strForcedValue = objXmlPower.Attributes["select"].InnerText;

				objPower.Create(objXmlCritterPower, objCharacter, objNode, intRating, strForcedValue);
				objCharacter.CritterPowers.Add(objPower);
			}

			// Set the Skill Ratings for the Critter.
			foreach (XmlNode objXmlSkill in objXmlCritter.SelectNodes("skills/skill"))
			{
				if (objXmlSkill.InnerText.Contains("Exotic"))
				{
					Skill objExotic = new Skill(objCharacter);
					objExotic.ExoticSkill = true;
					objExotic.Attribute = "AGI";
					if (objXmlSkill.Attributes["spec"] != null)
						objExotic.Specialization = objXmlSkill.Attributes["spec"].InnerText;
					if (Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(nudForce.Value), 0)) > 6)
						objExotic.RatingMaximum = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(nudForce.Value), 0));
					objExotic.Rating = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(nudForce.Value), 0));
					objExotic.Name = objXmlSkill.InnerText;
					objCharacter.Skills.Add(objExotic);
				}
				else
				{
					foreach (Skill objSkill in objCharacter.Skills)
					{
						if (objSkill.Name == objXmlSkill.InnerText)
						{
							if (objXmlSkill.Attributes["spec"] != null)
								objSkill.Specialization = objXmlSkill.Attributes["spec"].InnerText;
							if (Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(nudForce.Value), 0)) > 6)
								objSkill.RatingMaximum = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(nudForce.Value), 0));
							objSkill.Rating = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(nudForce.Value), 0));
							break;
						}
					}
				}
			}

			// Set the Skill Group Ratings for the Critter.
			foreach (XmlNode objXmlSkill in objXmlCritter.SelectNodes("skills/group"))
			{
				foreach (SkillGroup objSkill in objCharacter.SkillGroups)
				{
					if (objSkill.Name == objXmlSkill.InnerText)
					{
						objSkill.RatingMaximum = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(nudForce.Value), 0));
						objSkill.Rating = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(nudForce.Value), 0));
						break;
					}
				}
			}

			// Set the Knowledge Skill Ratings for the Critter.
			foreach (XmlNode objXmlSkill in objXmlCritter.SelectNodes("skills/knowledge"))
			{
				Skill objKnowledge = new Skill(objCharacter);
				objKnowledge.Name = objXmlSkill.InnerText;
				objKnowledge.KnowledgeSkill = true;
				if (objXmlSkill.Attributes["spec"] != null)
					objKnowledge.Specialization = objXmlSkill.Attributes["spec"].InnerText;
				objKnowledge.SkillCategory = objXmlSkill.Attributes["category"].InnerText;
				if (Convert.ToInt32(objXmlSkill.Attributes["rating"].InnerText) > 6)
					objKnowledge.RatingMaximum = Convert.ToInt32(objXmlSkill.Attributes["rating"].InnerText);
				objKnowledge.Rating = Convert.ToInt32(objXmlSkill.Attributes["rating"].InnerText);
				objCharacter.Skills.Add(objKnowledge);
			}

			// If this is a Critter with a Force (which dictates their Skill Rating/Maximum Skill Rating), set their Skill Rating Maximums.
			if (intForce > 0)
			{
				int intMaxRating = intForce;
				// Determine the highest Skill Rating the Critter has.
				foreach (Skill objSkill in objCharacter.Skills)
				{
					if (objSkill.RatingMaximum > intMaxRating)
						intMaxRating = objSkill.RatingMaximum;
				}

				// Now that we know the upper limit, set all of the Skill Rating Maximums to match.
				foreach (Skill objSkill in objCharacter.Skills)
					objSkill.RatingMaximum = intMaxRating;
				foreach (SkillGroup objGroup in objCharacter.SkillGroups)
					objGroup.RatingMaximum = intMaxRating;

				// Set the MaxSkillRating for the character so it can be used later when they add new Knowledge Skills or Exotic Skills.
				objCharacter.MaxSkillRating = intMaxRating;
			}

			// Add any Complex Forms the Critter comes with (typically Sprites)
			XmlDocument objXmlProgramDocument = XmlManager.Instance.Load("programs.xml");
			foreach (XmlNode objXmlComplexForm in objXmlCritter.SelectNodes("complexforms/complexform"))
			{
				int intRating = 0;
				if (objXmlComplexForm.Attributes["rating"] != null)
					intRating = Convert.ToInt32(ExpressionToString(objXmlComplexForm.Attributes["rating"].InnerText, Convert.ToInt32(nudForce.Value), 0));
				string strForceValue = "";
				if (objXmlComplexForm.Attributes["select"] != null)
					strForceValue = objXmlComplexForm.Attributes["select"].InnerText;
				XmlNode objXmlProgram = objXmlProgramDocument.SelectSingleNode("/chummer/programs/program[name = \"" + objXmlComplexForm.InnerText + "\"]");
				TreeNode objNode = new TreeNode();
				TechProgram objProgram = new TechProgram(objCharacter);
				objProgram.Create(objXmlProgram, objCharacter, objNode, strForceValue);
				objProgram.Rating = intRating;
				objCharacter.TechPrograms.Add(objProgram);

				// Add the Program Option if applicable.
				if (objXmlComplexForm.Attributes["option"] != null)
				{
					int intOptRating = 0;
					if (objXmlComplexForm.Attributes["optionrating"] != null)
						intOptRating = Convert.ToInt32(ExpressionToString(objXmlComplexForm.Attributes["optionrating"].InnerText, Convert.ToInt32(nudForce.Value), 0));
					string strOptForceValue = "";
					if (objXmlComplexForm.Attributes["optionselect"] != null)
						strOptForceValue = objXmlComplexForm.Attributes["optionselect"].InnerText;
					XmlNode objXmlOption = objXmlProgramDocument.SelectSingleNode("/chummer/options/option[name = \"" + objXmlComplexForm.Attributes["option"].InnerText + "\"]");
					TreeNode objNodeOpt = new TreeNode();
					TechProgramOption objOption = new TechProgramOption(objCharacter);
					objOption.Create(objXmlOption, objCharacter, objNodeOpt, strOptForceValue);
					objOption.Rating = intOptRating;
					objProgram.Options.Add(objOption);
				}
			}

			// Add any Gear the Critter comes with (typically Programs for A.I.s)
			XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");
			foreach (XmlNode objXmlGear in objXmlCritter.SelectNodes("gears/gear"))
			{
				int intRating = 0;
				if (objXmlGear.Attributes["rating"] != null)
					intRating = Convert.ToInt32(ExpressionToString(objXmlGear.Attributes["rating"].InnerText, Convert.ToInt32(nudForce.Value), 0));
				string strForceValue = "";
				if (objXmlGear.Attributes["select"] != null)
					strForceValue = objXmlGear.Attributes["select"].InnerText;
				XmlNode objXmlGearItem = objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlGear.InnerText + "\"]");
				TreeNode objNode = new TreeNode();
				Gear objGear = new Gear(objCharacter);
				List<Weapon> lstWeapons = new List<Weapon>();
				List<TreeNode> lstWeaponNodes = new List<TreeNode>();
				objGear.Create(objXmlGearItem, objCharacter, objNode, intRating, lstWeapons, lstWeaponNodes, strForceValue);
				objGear.Cost = "0";
				objGear.Cost3 = "0";
				objGear.Cost6 = "0";
				objGear.Cost10 = "0";
				objCharacter.Gear.Add(objGear);
			}

			// If this is a Mutant Critter, count up the number of Skill points they start with.
			if (objCharacter.MetatypeCategory == "Mutant Critters")
			{
				foreach (Skill objSkill in objCharacter.Skills)
					objCharacter.MutantCritterBaseSkills += objSkill.Rating;
			}

			// Add the Unarmed Attack Weapon to the character.
			try
			{
				objXmlDocument = XmlManager.Instance.Load("weapons.xml");
				XmlNode objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"Unarmed Attack\"]");
				TreeNode objDummy = new TreeNode();
				Weapon objWeapon = new Weapon(objCharacter);
				objWeapon.Create(objXmlWeapon, objCharacter, objDummy, null, null, null);
				objCharacter.Weapons.Add(objWeapon);
			}
			catch
			{
			}

			objCharacter.Alias = strCritterName;
			objCharacter.Created = true;
			objCharacter.Save();

			string strOpenFile = objCharacter.FileName;
			objCharacter = null;

			// Link the newly-created Critter to the Spirit.
			_objSpirit.FileName = strOpenFile;
			if (_objSpirit.EntityType == SpiritType.Spirit)
				tipTooltip.SetToolTip(imgLink, LanguageManager.Instance.GetString("Tip_Spirit_OpenFile"));
			else
				tipTooltip.SetToolTip(imgLink, LanguageManager.Instance.GetString("Tip_Sprite_OpenFile"));
			FileNameChanged(this);

			GlobalOptions.Instance.MainForm.LoadCharacter(strOpenFile, true);
		}

		/// <summary>
		/// Convert Force, 1D6, or 2D6 into a usable value.
		/// </summary>
		/// <param name="strIn">Expression to convert.</param>
		/// <param name="intForce">Force value to use.</param>
		/// <param name="intOffset">Dice offset.</param>
		/// <returns></returns>
		public string ExpressionToString(string strIn, int intForce, int intOffset)
		{
			int intValue = 0;
			XmlDocument objXmlDocument = new XmlDocument();
			XPathNavigator nav = objXmlDocument.CreateNavigator();
			XPathExpression xprAttribute = nav.Compile(strIn.Replace("/", " div ").Replace("F", intForce.ToString()).Replace("1D6", intForce.ToString()).Replace("2D6", intForce.ToString()));
			// This statement is wrapped in a try/catch since trying 1 div 2 results in an error with XSLT.
			try
			{
			    string temp = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", nav.Evaluate(xprAttribute));
                intValue = Convert.ToInt32(temp);
			}
			catch
			{
				intValue = 1;
			}
			intValue += intOffset;
			if (intForce > 0)
			{
				if (intValue < 1)
					intValue = 1;
			}
			else
			{
				if (intValue < 0)
					intValue = 0;
			}
			return intValue.ToString();
		}
		#endregion
    }
}