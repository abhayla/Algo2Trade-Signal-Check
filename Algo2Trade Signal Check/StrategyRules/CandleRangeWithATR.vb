﻿Imports Algo2TradeBLL
Imports System.Threading
Public Class CandleRangeWithATR
    Inherits Rule
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As Integer, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(ByVal startDate As Date, ByVal endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Trading Symbol")
        ret.Columns.Add("Open")
        ret.Columns.Add("Low")
        ret.Columns.Add("High")
        ret.Columns.Add("Close")
        ret.Columns.Add("Volume")
        ret.Columns.Add("Candle Range")
        ret.Columns.Add("ATR")
        ret.Columns.Add("CR ATR %")
        ret.Columns.Add("Slab")
        ret.Columns.Add("CR Slab %")

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
                            stockPayload = _cmn.GetRawPayload(_category, stock, chkDate.AddDays(-20), chkDate)
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

                        'Main Logic
                        If currentDayPayload IsNot Nothing AndAlso currentDayPayload.Count > 0 Then
                            Dim atrPayload As Dictionary(Of Date, Decimal) = Nothing
                            Indicator.ATR.CalculateATR(14, inputPayload, atrPayload)

                            Dim eodPayload As Dictionary(Of Date, Payload) = _cmn.GetRawPayload(Common.DataBaseTable.EOD_Cash, stock, chkDate.AddDays(-200), chkDate)
                            If eodPayload IsNot Nothing AndAlso eodPayload.Count > 100 Then
                                Dim eodatrPayload As Dictionary(Of Date, Decimal) = Nothing
                                Indicator.ATR.CalculateATR(14, eodPayload, eodatrPayload)

                                If eodatrPayload IsNot Nothing AndAlso eodatrPayload.ContainsKey(chkDate.Date) Then
                                    Dim slab As Decimal = CalculateSlab(currentDayPayload.Values.FirstOrDefault.Open, eodatrPayload(chkDate.Date))

                                    For Each runningPayload In currentDayPayload.Keys
                                        _canceller.Token.ThrowIfCancellationRequested()
                                        Dim row As DataRow = ret.NewRow
                                        row("Date") = inputPayload(runningPayload).PayloadDate
                                        row("Trading Symbol") = inputPayload(runningPayload).TradingSymbol
                                        row("Open") = inputPayload(runningPayload).Open
                                        row("Low") = inputPayload(runningPayload).Low
                                        row("High") = inputPayload(runningPayload).High
                                        row("Close") = inputPayload(runningPayload).Close
                                        row("Volume") = inputPayload(runningPayload).Volume
                                        row("Candle Range") = inputPayload(runningPayload).CandleRange
                                        row("ATR") = Math.Round(atrPayload(runningPayload), 4)
                                        row("CR ATR %") = Math.Round((inputPayload(runningPayload).CandleRange / atrPayload(runningPayload)) * 100, 4)
                                        row("Slab") = slab
                                        row("CR Slab %") = Math.Round((inputPayload(runningPayload).CandleRange / slab) * 100, 4)

                                        ret.Rows.Add(row)
                                    Next
                                End If
                            End If
                        End If
                    End If
                Next
            End If
            chkDate = chkDate.AddDays(1)
        End While
        Return ret
    End Function

    Private Function CalculateSlab(ByVal price As Decimal, ByVal atr As Decimal) As Decimal
        Dim ret As Decimal = 0.5
        Dim slabList As List(Of Decimal) = New List(Of Decimal) From {0.5, 1, 2.5, 5, 10, 15}
        Dim supportedSlabList As List(Of Decimal) = slabList.FindAll(Function(x)
                                                                         Return x <= atr / 8
                                                                     End Function)
        If supportedSlabList IsNot Nothing AndAlso supportedSlabList.Count > 0 Then
            ret = supportedSlabList.Max
            If price * 1 / 100 < ret Then
                Dim newSupportedSlabList As List(Of Decimal) = supportedSlabList.FindAll(Function(x)
                                                                                             Return x <= price * 1 / 100
                                                                                         End Function)
                If newSupportedSlabList IsNot Nothing AndAlso newSupportedSlabList.Count > 0 Then
                    ret = newSupportedSlabList.Max
                End If
            End If
        End If
        Return ret
    End Function
End Class
