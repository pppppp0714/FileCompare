using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using ListView = System.Windows.Forms.ListView;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

namespace FileCompare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            btnCopyFromLeft.Click += btnCopyFromLeft_Click;
            btnCopyFromRight.Click += btnCopyFromRight_Click;
        }

        private void btnCopyFromRight_Click(object? sender, EventArgs e)
        {
            if (lvwRightDir.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "복사할 항목을 선택하세요.", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtLeftDir.Text) || !Directory.Exists(txtLeftDir.Text))
            {
                MessageBox.Show(this, "대상 폴더 경로가 유효하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                foreach (ListViewItem item in lvwRightDir.SelectedItems)
                {
                    if (item.ForeColor == Color.Gray)
                    {
                        var ret = MessageBox.Show(this, "선택한 항목은 대상보다 이전 버전입니다. 계속 복사하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (ret != DialogResult.Yes) continue;
                    }

                    if (item.Tag is FileInfo fi)
                    {
                        var destPath = Path.Combine(txtLeftDir.Text, fi.Name);
                        File.Copy(fi.FullName, destPath, true);
                        File.SetLastWriteTime(destPath, fi.LastWriteTime);
                    }
                    else if (item.Tag is DirectoryInfo di)
                    {
                        var destDir = Path.Combine(txtLeftDir.Text, di.Name);
                        CopyDirectory(di.FullName, destDir);
                        Directory.SetLastWriteTime(destDir, di.LastWriteTime);
                    }
                }

                // 목록 갱신 및 색상 비교
                PopulateListView(lvwLeftDir, txtLeftDir.Text);
                CompareListViews();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "복사 중 오류가 발생했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCopyFromLeft_Click(object? sender, EventArgs e)
        {
            if (lvwLeftDir.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "복사할 항목을 선택하세요.", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtRightDir.Text) || !Directory.Exists(txtRightDir.Text))
            {
                MessageBox.Show(this, "대상 폴더 경로가 유효하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                foreach (ListViewItem item in lvwLeftDir.SelectedItems)
                {
                    if (item.ForeColor == Color.Gray)
                    {
                        var ret = MessageBox.Show(this, "선택한 항목은 대상보다 이전 버전입니다. 계속 복사하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (ret != DialogResult.Yes) continue;
                    }
                    if (item.Tag is FileInfo fi)
                    {
                        var destPath = Path.Combine(txtRightDir.Text, fi.Name);
                        File.Copy(fi.FullName, destPath, true);
                        File.SetLastWriteTime(destPath, fi.LastWriteTime);
                    }
                    else if (item.Tag is DirectoryInfo di)
                    {
                        var destDir = Path.Combine(txtRightDir.Text, di.Name);
                        CopyDirectory(di.FullName, destDir);
                        Directory.SetLastWriteTime(destDir, di.LastWriteTime);
                    }
                }

                // 목록 갱신 및 색상 비교
                PopulateListView(lvwRightDir, txtRightDir.Text);
                CompareListViews();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "복사 중 오류가 발생했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            var src = new DirectoryInfo(sourceDir);
            if (!src.Exists) return;
            Directory.CreateDirectory(destDir);

            // 파일 복사
            foreach (var file in src.GetFiles())
            {
                var destFile = Path.Combine(destDir, file.Name);
                file.CopyTo(destFile, true);
                File.SetLastWriteTime(destFile, file.LastWriteTime);
            }

            // 하위 디렉터리 재귀 복사
            foreach (var dir in src.GetDirectories())
            {
                var nextDest = Path.Combine(destDir, dir.Name);
                CopyDirectory(dir.FullName, nextDest);
                Directory.SetLastWriteTime(nextDest, dir.LastWriteTime);
            }
        }

        private void CompareListViews()
        {
            // 오른쪽 목록 항목들을 이름별로 빠르게 찾기 위한 딕셔너리 구성
            var rightDict = new Dictionary<string, ListViewItem>(StringComparer.OrdinalIgnoreCase);
            foreach (ListViewItem r in lvwRightDir.Items)
            {
                rightDict[r.Text] = r;
            }

            // 왼쪽 항목 처리
            foreach (ListViewItem l in lvwLeftDir.Items)
            {
                if (!rightDict.TryGetValue(l.Text, out var r))
                {
                    // 오른쪽에 없음 -> 추가된 항목(왼쪽에만 존재하는 항목으로 처리)
                    l.ForeColor = Color.Purple; // 보라
                }
                else
                {
                    // 양쪽에 모두 존재 -> Tag의 LastWriteTime으로 비교
                    if (l.Tag is FileSystemInfo lfi && r.Tag is FileSystemInfo rfi)
                    {
                        var lTime = lfi.LastWriteTime;
                        var rTime = rfi.LastWriteTime;
                        if (lTime > rTime)
                        {
                            // 왼쪽이 더 최신 -> 수정됨(왼쪽이 새로움)
                            l.ForeColor = Color.Red;
                            r.ForeColor = Color.Gray; // 기존(더이전)
                        }
                        else if (lTime < rTime)
                        {
                            // 오른쪽이 더 최신 -> 수정됨(오른쪽이 새로움)
                            r.ForeColor = Color.Red;
                            l.ForeColor = Color.Gray;
                        }
                        else
                        {
                            // 동일함
                            l.ForeColor = Color.Black;
                            r.ForeColor = Color.Black;
                        }
                    }
                    else
                    {
                        // Tag가 설정되지 않았으면 검정색으로 유지
                        l.ForeColor = Color.Black;
                        r.ForeColor = Color.Black;
                    }
                }
            }

            // 왼쪽에 없는 오른쪽 전용 항목
            var leftNames = new HashSet<string>(lvwLeftDir.Items.Cast<ListViewItem>().Select(i => i.Text), StringComparer.OrdinalIgnoreCase);
            foreach (ListViewItem r in lvwRightDir.Items)
            {
                if (!leftNames.Contains(r.Text))
                {
                    r.ForeColor = Color.Purple; // 오른쪽에만 존재 -> 추가된 항목
                }
            }
        }
        private void PopulateListView(ListView lv, string folderPath)
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            try
            { // 폴더(디렉터리) 먼저추가
                var dirs = Directory.EnumerateDirectories(folderPath)
                .Select(p => new DirectoryInfo(p))
                .OrderBy(d => d.Name);
                foreach (var d in dirs)
                {
                    var item = new ListViewItem(d.Name);
                    item.SubItems.Add("<DIR>");
                    item.SubItems.Add(d.LastWriteTime.ToString("g"));
                    // store FileSystemInfo for later comparison and default color
                    item.Tag = d;
                    item.ForeColor = Color.Black;
                    lv.Items.Add(item);
                }
                // 파일추가
                var files = Directory.EnumerateFiles(folderPath)
                .Select(p => new FileInfo(p))
                .OrderBy(f => f.Name);
                foreach (var f in files)
                {
                    var item = new ListViewItem(f.Name);
                    item.SubItems.Add(f.Length.ToString("N0") + " 바이트");
                    item.SubItems.Add(f.LastWriteTime.ToString("g"));
                    // store FileSystemInfo for later comparison and default color
                    item.Tag = f;
                    item.ForeColor = Color.Black;
                    lv.Items.Add(item);
                }
                // 컬럼너비자동조정(컨텐츠기준)
                for (int i = 0; i < lv.Columns.Count; i++)
                {
                    lv.AutoResizeColumn(i,
                    ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show(this, "폴더를찾을수없습니다.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, "입출력오류: " + ex.Message, "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lv.EndUpdate();
            }
        }

        
        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를선택하세요.";
                // 현재텍스트박스에있는경로를초기선택폴더로설정
                if (!string.IsNullOrWhiteSpace(txtLeftDir.Text) &&
                Directory.Exists(txtLeftDir.Text))
                {
                    dlg.SelectedPath = txtLeftDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;
                    PopulateListView(lvwLeftDir, dlg.SelectedPath);
                    CompareListViews();
                }

            }
        }

        private void btnRightDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를선택하세요.";
                // 현재텍스트박스에있는경로를초기선택폴더로설정
                if (!string.IsNullOrWhiteSpace(txtRightDir.Text) &&
                Directory.Exists(txtRightDir.Text))
                {
                    dlg.SelectedPath = txtRightDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;
                    PopulateListView(lvwRightDir, dlg.SelectedPath);
                    CompareListViews();
                }

            }
        }
    }
}
