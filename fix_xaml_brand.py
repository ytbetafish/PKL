p = r'Plain Craft Launcher 2\FormMain.xaml'
t = open(p, encoding='utf-8-sig').read()

# Hide ShapeTitleLogo
t = t.replace('''                            <Path Grid.Column="0"
                                  x:Name="ShapeTitleLogo"
                                  Data="M26,29 v-25 h6 a7,7 180 0 1 0,14 h-6 M62.5,6.5 a10,11.5 180 1 0 0,18 M71,2.5 v24.5 h13.5"
                                  SnapsToDevicePixels="False"''',
'''                            <Path Grid.Column="0"
                                  x:Name="ShapeTitleLogo"
                                  Visibility="Collapsed"
                                  Data="M26,29 v-25 h6 a7,7 180 0 1 0,14 h-6 M62.5,6.5 a10,11.5 180 1 0 0,18 M71,2.5 v24.5 h13.5"
                                  SnapsToDevicePixels="False"''')

# Hide ShapeHMCLTitleLogo
t = t.replace('''                            <Path Grid.Column="0"
                                  x:Name="ShapeHMCLTitleLogo"
                                  Data="M 60.375''',
'''                            <Path Grid.Column="0"
                                  x:Name="ShapeHMCLTitleLogo"
                                  Visibility="Collapsed"
                                  Data="M 60.375''')

# Show LabTitleLogo with text PKL
t = t.replace('''                            <TextBlock Grid.Column="0"
                                       x:Name="LabTitleLogo"
                                       Visibility="Collapsed"''',
'''                            <TextBlock Grid.Column="0"
                                       x:Name="LabTitleLogo"
                                       Text="PKL"
                                       Visibility="Visible"''')

# Hide CELogo
t = t.replace('''                            <Border x:Name="CELogo"
                                    Grid.Column="1"''',
'''                            <Border x:Name="CELogo"
                                    Visibility="Collapsed"
                                    Grid.Column="1"''')

open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
