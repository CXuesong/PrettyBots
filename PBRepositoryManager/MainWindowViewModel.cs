using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using PrettyBots.Strategies.Repository;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.NetEase;

namespace PBRepositoryManager
{
    internal class MainWindowViewModel : ObservableObject
    {
        private PrimaryRepository repos;
        private PrimaryRepositoryDbAdapter _RepostoryAdapter;
        private Visitor _Visitor;
        private string _VisitorUserName;
        private string _VisitorPassword;
        private Account _SelectedAccount;
        private Session _SelectedSession;

        public PrimaryRepositoryDbAdapter RepostoryAdapter
        {
            get { return _RepostoryAdapter; }
            private set
            {
                Set(ref _RepostoryAdapter, value);
                SubmitChangesCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand OpenDatabaseCommand { get; set; }

        public RelayCommand SubmitChangesCommand { get; set; }

        public Account SelectedAccount
        {
            get { return _SelectedAccount; }
            set
            {
                Set(ref _SelectedAccount, value);
                if (value != null)
                {
                    VisitorUserName = value.UserName;
                    VisitorPassword = value.Password;
                }
            }
        }

        public Session SelectedSession
        {
            get { return _SelectedSession; }
            set
            {
                Set(ref _SelectedSession, value);
                VisitorSaveSessionCommand.RaiseCanExecuteChanged();
                CreateVisitorCommand.RaiseCanExecuteChanged();
            }
        }

        #region Visitors

        public ObservableCollection<VisitorInfo> Visitors { get; private set; }

        public CollectionView VisitorsView { get; private set; }

        public Visitor Visitor
        {
            get { return _Visitor; }
            set
            {
                Set(ref _Visitor, value);
                VisitorLoginCommand.RaiseCanExecuteChanged();
                VisitorLogoutCommand.RaiseCanExecuteChanged();
                VisitorClearCookiesCommand.RaiseCanExecuteChanged();
                VisitorSaveSessionCommand.RaiseCanExecuteChanged();
            }
        }

        public bool VisitorPresented
        {
            get { return _Visitor != null; }
        }

        public string VisitorAccountInfo
        {
            get
            {
                var v = (IVisitor) _Visitor;
                return v != null && v.AccountInfo != null ? v.AccountInfo.ToString() : null;
            }
        }

        private void NotifyAccountInfoChanged()
        {
            RaisePropertyChanged("VisitorAccountInfo");
        }

        public string VisitorUserName
        {
            get { return _VisitorUserName; }
            set { Set(ref _VisitorUserName, value); }
        }

        public string VisitorPassword
        {
            get { return _VisitorPassword; }
            set { Set(ref _VisitorPassword, value); }
        }

        public RelayCommand CreateVisitorCommand { get; set; }

        public RelayCommand VisitorLoginCommand { get; set; }

        public RelayCommand VisitorLogoutCommand { get; set; }

        public RelayCommand VisitorClearCookiesCommand { get; set; }

        public RelayCommand VisitorSaveSessionCommand { get; set; }

        #endregion

        public MainWindowViewModel()
        {
            OpenDatabaseCommand = new RelayCommand(() =>
            {
                var ofd = new OpenFileDialog() { Filter = "*.mdf|*.mdf" };
                if (ofd.ShowDialog() == true)
                {
                    try
                    {
                        var nr =
                            new PrimaryRepository(
                                string.Format(
                                    "Data Source=(LocalDB)\\v11.0;AttachDbFilename=\"{0}\";Integrated Security=True;Connect Timeout=30",
                                    ofd.FileName));
                        var na = new PrimaryRepositoryDbAdapter(nr);
                        if (!na.DatabaseExists)
                        {
                            MessageBox.Show("数据库不存在。");
                            return;
                        }
                        if (repos != null) repos.Dispose();
                        repos = nr;
                        RepostoryAdapter = na;
                    }
                    catch (Exception ex)
                    {
                        Utility.ReportException(ex);
                    }
                }
            });
            SubmitChangesCommand = new RelayCommand(() =>
            {
                try
                {
                    RepostoryAdapter.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Utility.ReportException(ex);
                }
            }, () => RepostoryAdapter != null);
            Visitors = new ObservableCollection<VisitorInfo>()
            {
                new VisitorInfo(typeof (BaiduVisitor)),
                new VisitorInfo(typeof (LofterVisitor))
            };
            VisitorsView = new CollectionView(Visitors);
            CreateVisitorCommand = new RelayCommand(() =>
            {
                try
                {
                    var vi = VisitorsView.CurrentItem as VisitorInfo;
                    if (vi == null) return;
                    Visitor = vi.CreateVisitor();
                    Visitor.Session = SelectedSession.LoadSession();
                    Visitor.Session.VerificationCodeRecognizer = new InteractiveVCodeRecognizer();
                    NotifyAccountInfoChanged();
                }
                catch (Exception ex)
                {
                    Utility.ReportException(ex);
                }
            }, () => SelectedSession != null);
            VisitorLoginCommand = new RelayCommand(() =>
            {
                try
                {
                    Visitor.AccountInfo.Login(VisitorUserName, VisitorPassword);
                    var u = Visitor.AccountInfo as IUpdatable;
                    if (u != null) u.Update();
                    NotifyAccountInfoChanged();
                }
                catch (Exception ex)
                {
                    Utility.ReportException(ex);
                }
            }, () => Visitor != null);
            VisitorLogoutCommand = new RelayCommand(() =>
            {
                try
                {
                    Visitor.AccountInfo.Logout();
                    var u = Visitor.AccountInfo as IUpdatable;
                    if (u != null) u.Update();
                    NotifyAccountInfoChanged();
                }
                catch (Exception ex)
                {
                    Utility.ReportException(ex);
                }
            }, () => Visitor != null);
            VisitorClearCookiesCommand = new RelayCommand(() =>
            {
                try
                {
                    SelectedSession.ClearCookies();
                }
                catch (Exception ex)
                {
                    Utility.ReportException(ex);
                }
            }, () => SelectedSession != null);
            VisitorSaveSessionCommand = new RelayCommand(() =>
            {
                try
                {
                    SelectedSession.SaveSession(Visitor.Session);
                }
                catch (Exception ex)
                {
                    Utility.ReportException(ex);
                }
            }, () => SelectedSession != null && Visitor != null);
        }
    };

    internal class VisitorInfo
    {
        public Type VisitorType { get; private set; }

        public Visitor CreateVisitor()
        {
            return (Visitor)Activator.CreateInstance(VisitorType);
        }

        public override string ToString()
        {
            return VisitorType.Name;
        }

        public VisitorInfo(Type visitorType)
        {
            if (visitorType == null) throw new ArgumentNullException("visitorType");
            VisitorType = visitorType;
        }
    }
}
