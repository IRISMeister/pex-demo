# オリジナルからの変更点
下記の変更を加えました。
1. タイムゾーンをTokyoに変更
2. iris/src/dc/BusinessProcessBPL.clsを追加
3. Consumerで取得するkafkaメッセージ関連の情報(offset等)を増加
4. Requestメッセージを、[Producer用](iris/src/dc/ProducerRequest.cls),[Consumer用](iris/src/dc/ConsumerRequest.cls)に分離
5. [Cousumerの処理](dotnet/KafkaConsumer.cs)に、偶数回・奇数回でメッセージ形式を変えるロジックを追加

# 実行方法

## 起動方法
```bash
$ docker-compose up -d
```
主要なURLは下記の通りです。以下、コンテナの実行ホスト名をirishostとしています。

|用途|URL|クレデンシャル|
|:--|:--|:--|
|IRIS管理ポータル | http://irishost:52773/csp/user/EnsPortal.ProductionConfig.zen | SuperUser/SYS |
|IRISプロダクション | http://irishost:52773/csp/user/EnsPortal.ProductionConfig.zen | SuperUser/SYS |
|Eco Kafka Manager | http://irishost:8082/brokers | N/A |

## 停止(削除)方法
```bash
$ docker-compose down
```


## Kafkaへの送信
KafkaProducerビジネスホストを選択し、テスト機能を使用して、メッセージ送信を行います。

|||
|:--|:--|
|リクエストタイプ|dc.ProducerRequest|
|Topic|test|
|Text| 任意|
![1](https://raw.githubusercontent.com/IRISMeister/doc-images/main/pex-demo/test-screen.png)

実行後、メッセージトレースを使用として、指定したデータが送信された事を確認してください。  
![1](https://raw.githubusercontent.com/IRISMeister/doc-images/main/pex-demo/test-screen-trace.png)

## Kafka Manager
Kafka Managerで、Topic:testにキューされたデータを閲覧します。
Resources > Topics > test > Browse Data > Fetch
![2](https://raw.githubusercontent.com/IRISMeister/doc-images/main/pex-demo/kafka.png)

## Kafkaからの受信
KafkaConsumerビジネスホストを有効化します。ポーリング間隔経過後(0.1秒毎)、受信処理が実行されます。
有効化した後、メッセージトレースを確認ください。dc.ConsumerRequestが受信されているはずです。  

![3](https://raw.githubusercontent.com/IRISMeister/doc-images/main/pex-demo/ConsumerRequest-1.png)  
ここで、%jsonプロパティの内容は、IRIS内からは下記のように取得可能です。
```ObjectScript
USER>s m=##class(EnsLib.PEX.Message).%OpenId(3)
USER>w m.%jsonObject.topic
test
USER>w m.%jsonObject.text
テストデータ
```

「Kafkaへの送信」以降の操作を繰り返すと、異なるメッセージの受信方法が交互に使用されます。先ほどとは異なり、この時点でメッセージの内容が各プロパティとして格納されています。  
![4](https://raw.githubusercontent.com/IRISMeister/doc-images/main/pex-demo/ConsumerRequest-2.png)

テスト機能を使用する代わりに、下記で、50件のメッセージを送信する事が出来ます。
```bash
$ docker-compose exec iris iris session iris "##class(dc.Util).send(50)"
```


なお、異なるtopicに変更する場合は、KafkaConsumerビジネスサービスの[Remote BusinessService]->[リモート設定]を変更します。

# 補足
1. .NETのコンパイル・実行には[.NET Core](https://docs.intersystems.com/iris20211/csp/docbookj/DocBook.UI.Page.cls?KEY=BNET_config#BNET_config_coretwo)を使用しています。

2. Kafka Managerは下記を使用しています。  
https://github.com/epam/eco-kafka-manager  
https://hub.docker.com/r/epam/eco-kafka-manager/tags

3. External Language Serversは別コンテナです。  
PEXはIRISと他言語の間の通信にGatewayを使用します。本デモでは、.NET用とJava用の2つのGatewayを使用します。通常これらは[管理ポータル](https://docs.intersystems.com/iris20211/csp/docbookj/DocBook.UI.Page.cls?KEY=EPEX_object_gateway)で、定義・実行しますが、本デモは手順を自動化するために、独立したコンテナとしてこれらを起動しています。  
PEXについてのマニュアルは[こちら](https://docs.intersystems.com/iris20211/csp/docbookj/DocBook.UI.Page.cls?KEY=EPEX)をご覧ください。

4. 受信メッセージの形式  
[メッセージ](https://docs.intersystems.com/iris20211/csp/docbookj/DocBook.UI.Page.cls?KEY=EPEX_hosts_adapters#EPEX_hosts_adapters_messaging)は、External Language側で定義する方法と、IRIS側で定義する方法があります。本デモでは[実装コード内](dotnet/KafkaConsumer.cs)でメッセージを受信するたびに、交互に切り替えています。  

