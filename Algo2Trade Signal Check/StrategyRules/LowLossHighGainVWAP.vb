Imports Algo2TradeBLL
Imports System.Threading
Imports Utilities.DAL
Imports Utilities.Numbers

Public Class LowLossHighGainVWAP
    Inherits Rule
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As Integer, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(ByVal startDate As Date, ByVal endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Instrument")
        ret.Columns.Add("Signal")
        ret.Columns.Add("Entry Price")
        ret.Columns.Add("Stoploss Price")
        ret.Columns.Add("Capital")
        ret.Columns.Add("Profit&Loss")
        ret.Columns.Add("Tag")
        Dim stockData As StockSelection = New StockSelection(_canceller, _category, _cmn, _fileName)
        AddHandler stockData.Heartbeat, AddressOf OnHeartbeat
        AddHandler stockData.WaitingFor, AddressOf OnWaitingFor
        AddHandler stockData.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        AddHandler stockData.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
        Dim chkDate As Date = startDate
        While chkDate <= endDate
            _canceller.Token.ThrowIfCancellationRequested()
            Dim stockList As List(Of String) = Nothing
            If _instrumentName Is Nothing OrElse _instrumentName = "" Then
                stockList = Await stockData.GetStockList(chkDate).ConfigureAwait(False)
            Else
                stockList = New List(Of String)
                stockList.Add(_instrumentName)
            End If
            _canceller.Token.ThrowIfCancellationRequested()
            If stockList IsNot Nothing AndAlso stockList.Count > 0 Then
                For Each stock In stockList
                    _canceller.Token.ThrowIfCancellationRequested()
                    Dim stockPayload As Dictionary(Of Date, Payload) = Nothing
                    Select Case _category
                        Case Common.DataBaseTable.Intraday_Cash, Common.DataBaseTable.Intraday_Commodity, Common.DataBaseTable.Intraday_Currency, Common.DataBaseTable.Intraday_Futures
                            stockPayload = _cmn.GetRawPayload(_category, stock, chkDate.AddDays(-8), chkDate)
                        Case Common.DataBaseTable.EOD_Cash, Common.DataBaseTable.EOD_Commodity, Common.DataBaseTable.EOD_Currency, Common.DataBaseTable.EOD_Futures, Common.DataBaseTable.EOD_POSITIONAL
                            stockPayload = _cmn.GetRawPayload(_category, stock, chkDate.AddDays(-200), chkDate)
                    End Select
                    _canceller.Token.ThrowIfCancellationRequested()
                    If stockPayload IsNot Nothing AndAlso stockPayload.Count > 0 Then
                        Dim XMinutePayload As Dictionary(Of Date, Payload) = Nothing
                        If _timeFrame > 1 Then
                            Dim exchangeStartTime As Date = New Date(chkDate.Year, chkDate.Month, chkDate.Day, 9, 15, 0)
                            XMinutePayload = Common.ConvertPayloadsToXMinutes(stockPayload, _timeFrame, exchangeStartTime)
                        Else
                            XMinutePayload = stockPayload
                        End If
                        _canceller.Token.ThrowIfCancellationRequested()
                        Dim inputPayload As Dictionary(Of Date, Payload) = Nothing
                        If _useHA Then
                            Indicator.HeikenAshi.ConvertToHeikenAshi(XMinutePayload, inputPayload)
                        Else
                            inputPayload = XMinutePayload
                        End If
                        _canceller.Token.ThrowIfCancellationRequested()
                        Dim currentDayPayload As Dictionary(Of Date, Payload) = Nothing
                        For Each runningPayload In inputPayload.Keys
                            _canceller.Token.ThrowIfCancellationRequested()
                            If runningPayload.Date = chkDate.Date Then
                                If currentDayPayload Is Nothing Then currentDayPayload = New Dictionary(Of Date, Payload)
                                currentDayPayload.Add(runningPayload, inputPayload(runningPayload))
                            End If
                        Next

                        If currentDayPayload IsNot Nothing AndAlso currentDayPayload.Count > 0 Then
                            'Supporting Start
                            Dim VWAPPayload As Dictionary(Of Date, Decimal) = Nothing
                            Indicator.VWAP.CalculateVWAP(inputPayload, VWAPPayload)
                            Dim lotSize As Integer = GetLotSize(stock, chkDate.Date)
                            'Supporting End
                            'Loop: Main Logic Start
                            For Each runningPayload In currentDayPayload.Keys
                                _canceller.Token.ThrowIfCancellationRequested()
                                If runningPayload.Date = currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate.Date Then
                                    Dim entryPrice As Decimal = 0
                                    Dim slPrice As Decimal = 0
                                    Dim signal As Integer = 0
                                    If currentDayPayload(runningPayload).PreviousCandlePayload.High >= VWAPPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) AndAlso
                                        currentDayPayload(runningPayload).PreviousCandlePayload.Low <= VWAPPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) Then
                                        If currentDayPayload(runningPayload).High > currentDayPayload(runningPayload).PreviousCandlePayload.High AndAlso
                                            currentDayPayload(runningPayload).Low < currentDayPayload(runningPayload).PreviousCandlePayload.Low Then
                                            If currentDayPayload(runningPayload).CandleColor = Color.Red Then
                                                entryPrice = currentDayPayload(runningPayload).PreviousCandlePayload.High
                                                slPrice = VWAPPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate)
                                                If entryPrice > slPrice Then signal = 1
                                            Else
                                                entryPrice = currentDayPayload(runningPayload).PreviousCandlePayload.Low
                                                slPrice = VWAPPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate)
                                                If entryPrice < slPrice Then signal = -1
                                            End If
                                        ElseIf currentDayPayload(runningPayload).High > currentDayPayload(runningPayload).PreviousCandlePayload.High Then
                                            entryPrice = currentDayPayload(runningPayload).PreviousCandlePayload.High
                                            slPrice = VWAPPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate)
                                            If entryPrice > slPrice Then signal = 1
                                        ElseIf currentDayPayload(runningPayload).Low < currentDayPayload(runningPayload).PreviousCandlePayload.Low Then
                                            entryPrice = currentDayPayload(runningPayload).PreviousCandlePayload.Low
                                            slPrice = VWAPPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate)
                                            If entryPrice < slPrice Then signal = -1
                                        End If
                                        If signal <> 0 Then
                                            Dim specialSignal As Integer = 0
                                            Dim pl As Decimal = 0
                                            If signal = 1 AndAlso
                                                currentDayPayload(runningPayload).PreviousCandlePayload.CandleWicks.Top <= currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange / 4 AndAlso
                                                currentDayPayload(runningPayload).PreviousCandlePayload.CandleColor = Color.Green Then
                                                entryPrice += CalculateBuffer(entryPrice, RoundOfType.Floor)
                                                pl = CalculatePL(entryPrice, slPrice, lotSize)
                                                If (currentDayPayload(runningPayload).PreviousCandlePayload.High - slPrice) <= currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange / 3 Then
                                                    specialSignal = 1
                                                End If
                                            ElseIf signal = -1 AndAlso
                                                currentDayPayload(runningPayload).PreviousCandlePayload.CandleWicks.Bottom <= currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange / 4 AndAlso
                                                currentDayPayload(runningPayload).PreviousCandlePayload.CandleColor = Color.Red Then
                                                entryPrice -= CalculateBuffer(entryPrice, RoundOfType.Floor)
                                                pl = CalculatePL(slPrice, entryPrice, lotSize)
                                                If (slPrice - currentDayPayload(runningPayload).PreviousCandlePayload.Low) <= currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange / 3 Then
                                                    specialSignal = -1
                                                End If
                                            End If
                                            Dim capital As Decimal = Math.Round(entryPrice * lotSize / 13, 2)
                                            If pl <> 0 AndAlso Math.Abs(pl) <= capital * 5 / 100 Then
                                                Dim row As DataRow = ret.NewRow
                                                row("Date") = currentDayPayload(runningPayload).PayloadDate
                                                row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                                row("Signal") = signal
                                                row("Entry Price") = entryPrice
                                                row("Stoploss Price") = slPrice
                                                row("Capital") = capital
                                                row("Profit&Loss") = pl
                                                row("Tag") = "Capital"
                                                ret.Rows.Add(row)
                                            End If
                                            If specialSignal <> 0 Then
                                                Dim row As DataRow = ret.NewRow
                                                row("Date") = currentDayPayload(runningPayload).PayloadDate
                                                row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                                row("Signal") = specialSignal
                                                row("Entry Price") = entryPrice
                                                row("Stoploss Price") = slPrice
                                                row("Capital") = capital
                                                row("Profit&Loss") = pl
                                                row("Tag") = "CandleRange"
                                                ret.Rows.Add(row)
                                            End If
                                        End If
                                    End If
                                End If
                            Next
                            'Main Logic End
                        End If
                    End If
                Next
            End If
            chkDate = chkDate.AddDays(1)
        End While
        Return ret
    End Function

    Private Function CalculatePL(ByVal buyPrice As Double, ByVal sellPrice As Double, ByVal quantity As Integer) As Decimal
        Dim potentialBrokerage As New Calculator.BrokerageAttributes
        Dim calculator As New Calculator.BrokerageCalculator(Nothing)
        calculator.Intraday_Equity(buyPrice, sellPrice, quantity, potentialBrokerage)
        Return potentialBrokerage.NetProfitLoss
    End Function

    Private Function GetLotSize(ByVal stock As String, ByVal tradingDate As Date) As Integer
        Dim ret As Integer = Nothing
        Dim statement As String = String.Format("SELECT DISTINCT `LOT_SIZE` FROM `active_instruments_futures` WHERE `INSTRUMENT_NAME`='{0}' AND `SEGMENT`='NFO-FUT' AND `AS_ON_DATE`>='{1}' AND `AS_ON_DATE`<='{2}'",
                                                stock, tradingDate.AddDays(-30).Date.ToString("yyyy-MM-dd"), tradingDate.Date.ToString("yyyy-MM-dd"))
        Using dbConn As New MySQLDBHelper("DESKTOP-RIO", "local_stock", 3306, "rio", "speech123", _canceller)
            AddHandler dbConn.Heartbeat, AddressOf OnHeartbeat
            AddHandler dbConn.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            AddHandler dbConn.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
            AddHandler dbConn.WaitingFor, AddressOf OnWaitingFor

            dbConn.OpenConnection()
            Dim dt As New DataTable
            dt = dbConn.RunSelect(statement)
            If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
                ret = dt.Rows(0).Item(0)
            End If
        End Using
        Return ret
    End Function
End Class
