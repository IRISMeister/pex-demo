Class dc.Util
{

ClassMethod send(loop = 100)
{
    #dim m as dc.ProducerRequest

	Set tSC = ##class(Ens.Director).CreateBusinessService("EnsLib.Testing.Service",.tService)
	If $$$ISERR(tSC) { Write "Error :"_tSC,! Return }

    For i=1:1:loop {
        Set tTestRequest = ##class(EnsLib.Testing.Request).%New() 
        Set tTestRequest.Target = "KafkaProducer"
        Set tTestRequest.SyncCall = 1        
        Set tRequest=##class(dc.ProducerRequest).%New()
        Set tRequest.Topic="test", tRequest.Text="テストメッセージ:"_i
        Set tTestRequest.Request = tRequest
        Set tSC = tService.ProcessInput(tTestRequest, .tResponse) Quit:$$$ISERR(tSC)
        w "offset is "_tResponse.StringValue,!
    }
}

}
