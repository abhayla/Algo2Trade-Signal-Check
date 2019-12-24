Namespace Indicator
    Public Module FractalBandsTrendLine
        Public Sub CalculateFractalBandsTrendLine(ByVal inputPayload As Dictionary(Of Date, Payload), ByRef outputHighPayload As Dictionary(Of Date, Decimal), ByRef outputLowPayload As Dictionary(Of Date, Decimal))
            If inputPayload IsNot Nothing AndAlso inputPayload.Count > 0 Then
                Dim fractalHighPayload As Dictionary(Of Date, Decimal) = Nothing
                Dim fractalLowPayload As Dictionary(Of Date, Decimal) = Nothing
                Indicator.FractalBands.CalculateFractal(inputPayload, fractalHighPayload, fractalLowPayload)
                For Each runningPayload In inputPayload
                    Dim firstUCandle As Payload = GetFractalUFormingCandle(inputPayload, fractalHighPayload, runningPayload.Key, 1)
                Next
            End If
        End Sub
        Private Function GetFractalUFormingCandle(ByVal inputPayload As Dictionary(Of Date, Payload), ByVal fractalPayload As Dictionary(Of Date, Decimal), ByVal beforeThisTime As Date, ByVal direction As Integer) As Payload
            Dim ret As Payload = Nothing
            If fractalPayload IsNot Nothing AndAlso fractalPayload.Count > 0 Then
                Dim checkingPayload As IEnumerable(Of KeyValuePair(Of Date, Decimal)) = fractalPayload.Where(Function(x)
                                                                                                                 Return x.Key <= beforeThisTime
                                                                                                             End Function)
                If checkingPayload IsNot Nothing AndAlso checkingPayload.Count > 0 Then
                    Dim firstCandleTime As Date = Date.MinValue
                    Dim middleCandleTime As Date = Date.MinValue
                    Dim lastCandleTime As Date = Date.MinValue
                    For Each runningPayload In checkingPayload.OrderByDescending(Function(x)
                                                                                     Return x.Key
                                                                                 End Function)
                        If direction = 1 Then
                            If firstCandleTime = Date.MinValue Then
                                firstCandleTime = runningPayload.Key
                            Else
                                If fractalPayload(firstCandleTime) >= runningPayload.Value Then
                                    firstCandleTime = runningPayload.Key
                                Else
                                    middleCandleTime = runningPayload.Key
                                End If
                            End If
                            If middleCandleTime <> Date.MinValue Then
                                If fractalPayload(middleCandleTime) = runningPayload.Value Then
                                    middleCandleTime = runningPayload.Key
                                ElseIf fractalPayload(middleCandleTime) < runningPayload.Value Then
                                    middleCandleTime = Date.MinValue
                                    firstCandleTime = runningPayload.Key
                                Else
                                    lastCandleTime = runningPayload.Key
                                    Exit For
                                End If
                            End If
                        End If
                    Next
                    If lastCandleTime <> Date.MinValue Then
                        For Each runningPayload In inputPayload.OrderByDescending(Function(x)
                                                                                      Return x.Key
                                                                                  End Function)
                            If runningPayload.Key < lastCandleTime Then
                                If direction = 1 Then
                                    If runningPayload.Value.High = fractalPayload(middleCandleTime) Then
                                        ret = runningPayload.Value
                                        Exit For
                                    End If
                                End If
                            End If
                        Next
                    End If
                End If
            End If
            Return ret
        End Function
    End Module
End Namespace