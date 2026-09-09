import re

with open('Views/PlanilhamentoPage.xaml', 'r', encoding='utf-8') as f:
    code = f.read()

# Replace the ScrollView row 5
pattern = r'<!-- Corpo da tabela \(card centralizado\) -->.*?<!-- RodapAc -->'
replacement = '''<!-- Corpo da tabela (card centralizado) -->
        <Border Grid.Row="5" Margin="20,8"
                StrokeShape="RoundRectangle 6"
                Stroke="{AppThemeBinding Light={StaticResource Border}, Dark={StaticResource BorderDark}}"
                StrokeThickness="1"
                BackgroundColor="{AppThemeBinding Light={StaticResource Surface}, Dark={StaticResource SurfaceDark}}"
                Padding="0"
                HorizontalOptions="Center"
                VerticalOptions="Fill">
            <ScrollView Orientation="Horizontal" HorizontalScrollBarVisibility="Default" VerticalOptions="Fill">
                <VerticalStackLayout Spacing="0" VerticalOptions="Fill">
                    
                    <!-- HEADER -->
                    <HorizontalStackLayout BackgroundColor="#1154A8" HeightRequest="40">
                        <Label Text="CÓDIGO" FontSize="11" FontAttributes="Bold" TextColor="White" VerticalOptions="Center" WidthRequest="110" Padding="12,0"/>
                        <Label Text="DESCRIÇÃO" FontSize="11" FontAttributes="Bold" TextColor="White" VerticalOptions="Center" WidthRequest="380" Padding="12,0"/>
                        <HorizontalStackLayout BindableLayout.ItemsSource="{Binding Periodos}">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate>
                                    <Label Text="{Binding LabelCompleto}" FontSize="11" FontAttributes="Bold" TextColor="White" VerticalOptions="Center" HorizontalTextAlignment="End" WidthRequest="170" Padding="0,0,16,0"/>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                        </HorizontalStackLayout>
                    </HorizontalStackLayout>

                    <!-- BODY -->
                    <CollectionView ItemsSource="{Binding ItensTabela}" VerticalOptions="FillAndExpand">
                        <CollectionView.ItemTemplate>
                            <DataTemplate>
                                <Grid>
                                    <!-- LINHA DE CONTA -->
                                    <Grid IsVisible="{Binding IsLinhaConta}">
                                        <HorizontalStackLayout>
                                            <Label Text="{Binding Linha.Codigo}" FontFamily="Consolas" FontSize="12" FontAttributes="{Binding Linha.Estilo}" VerticalOptions="Center" LineBreakMode="NoWrap" WidthRequest="110" Padding="12,8,8,8"/>
                                            <Label Text="{Binding Linha.Descricao}" FontSize="12" FontAttributes="{Binding Linha.Estilo}" VerticalOptions="Center" WidthRequest="380" Padding="{Binding Linha.Indentacao}"/>
                                            
                                            <HorizontalStackLayout BindableLayout.ItemsSource="{Binding Linha.CelulasVisiveis}">
                                                <BindableLayout.ItemTemplate>
                                                    <DataTemplate>
                                                        <Border WidthRequest="170" HeightRequest="32" Margin="6,6,16,6"
                                                                Stroke="{AppThemeBinding Light=#C5CCD6, Dark=#3A4554}" StrokeThickness="1"
                                                                BackgroundColor="{AppThemeBinding Light=White, Dark=#13202B}">
                                                            <Border.StrokeShape>
                                                                <RoundRectangle CornerRadius="4"/>
                                                            </Border.StrokeShape>
                                                            <Entry Text="{Binding ValorFormatado, Mode=OneWay}"
                                                                   Placeholder="0,00"
                                                                   Keyboard="Numeric"
                                                                   HorizontalTextAlignment="End"
                                                                   FontSize="13"
                                                                   BackgroundColor="Transparent"
                                                                   Margin="8,0"
                                                                   TextChanged="OnCelulaTextChanged"
                                                                   Unfocused="OnCelulaUnfocused"/>
                                                        </Border>
                                                    </DataTemplate>
                                                </BindableLayout.ItemTemplate>
                                            </HorizontalStackLayout>
                                        </HorizontalStackLayout>
                                        <!-- Borda inferior -->
                                        <BoxView HeightRequest="1" VerticalOptions="End" Color="{AppThemeBinding Light=#E1E5EB, Dark=#2A3441}"/>
                                    </Grid>
                                    
                                    <!-- BOTÃO ADICIONAR -->
                                    <Grid IsVisible="{Binding IsBotaoAdicionar}">
                                        <HorizontalStackLayout>
                                            <BoxView WidthRequest="110" Color="Transparent"/>
                                            <Button Text="+ Adicionar conta"
                                                    FontSize="12" HeightRequest="30" Padding="10,0"
                                                    HorizontalOptions="Start" Margin="24,2,0,4"
                                                    BackgroundColor="Transparent"
                                                    TextColor="{AppThemeBinding Light=#1F6FEB, Dark=#5A9BFF}"
                                                    Clicked="OnAdicionarContaClicked"
                                                    CommandParameter="{Binding ContaPaiIdParaAdicionar}" />
                                        </HorizontalStackLayout>
                                    </Grid>
                                </Grid>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>
                </VerticalStackLayout>
            </ScrollView>
        </Border>

        <!-- Rodapé -->'''

code = re.sub(pattern, replacement, code, flags=re.DOTALL)

with open('Views/PlanilhamentoPage.xaml', 'w', encoding='utf-8') as f:
    f.write(code)
