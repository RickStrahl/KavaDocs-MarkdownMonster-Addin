using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace KavaDocsAddin
{
    public class KavaDocsMenuHandler
    {
        public KavaDocsModel Model { get; set; }

        public MenuItem KavaDocsMenuItem { get; set; }

        public KavaDocsMenuHandler()
        {
            Model = kavaUi.AddinModel;
        }
        public MenuItem CreateKavaDocsMainMenu()
        {

            var mi = Model.TopicsTree.Resources["MainMenuKavaDocsMenu"] as MenuItem;
            mi.DataContext = Model.TopicsTree.Model;

            //var item = Model.TopicsTree.FindResource("MainMenuKavaDocsMenu");
            //if (item == null)
                

            //var mi = new MenuItem
            //{
            //    Header = "_Kava Docs",                
            //};
            //mi.Items.Add(new MenuItem
            //{
            //    Header = "_Open Project",
            //    Command = Model.Commands.OpenProjectCommand
            //});
            //mi.Items.Add(new MenuItem
            //{
            //    Header = "_Save Project",
            //    Command = Model.Commands.SaveProjectCommand
            //});
            //mi.Items.Add(new MenuItem
            //{
            //    Header = "_Close Project",
            //    Command = Model.Commands.CloseProjectCommand
            //});

            // insert Item after MainMenuEdit item on Main menu
            Model.Addin.AddMenuItem(mi, "MainMenuTools",mode: 0);

            KavaDocsMenuItem = mi;

            return mi;
        }

    }
}
