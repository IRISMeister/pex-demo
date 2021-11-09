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

# WindowsでPEX/.NET Framework 4.5を使用する例
Windows環境でPEX/.NETをビルド、起動する方法です。
## ビルド手順
使用IDE: Microsoft Visual Studio 2017  
1. 開く->プロジェクトで[PEX.csproj](dotnetfw45/PEX/PEX.csproj)を選択する。
2. 下記の参照設定が正しいことを確認します。 
- InterSystems.Data.IRISClient
- InterSystems.Data.Utils

これらが正しく参照出来ていない場合は、プロジェクトのプロパティで参照パスを追加する等して対処します。
> 参照すべき場所はIRISのインストール先がc:\InterSystems\IRISの場合は、C:\InterSystems\IRIS\dev\dotnet\bin\v4.5\

3. 新規作成されたソリューション(.sln)を保存。

4. Package Manager ConsoleでKafka .NET clientをインストール
```
PM> Install-Package Confluent.Kafka
```

5. ビルド実行

bin/Release/PEX.dllが生成されます。

## 実行

1. docker環境を起動
```bash
$ docker-compose up -d
$ docker-compose stop netgw
```
> 混乱を避けるために、コンテナ版のDotNet Gatewayだけを停止します。

2. DotNet Gatewayを単独で起動
```DOS
>C:\InterSystems\IRIS\dev\dotnet\bin\v4.5\InterSystems.Data.Gateway64 55556 "" "" 0.0.0.0
```
> 55556はListenするポート番号。値は任意。  
> Firewallの設定でIRIS ADO DotNet Gatewayによる外部通信を許可すること。  
> IRISもWinodws版を使用している場合は、代わりにExternal Language Serversを使用することもできます。  

3. プロダクション設定を変更

KafkaConsumerの  
[Remote BusinessService]->[ゲートウェイホスト]をWindowsホストのIPアドレスに変更。
> IRISをコンテナ実行している場合は、コンテナ内からアクセス可能なIPアドレスを指定すること

[Remote BusinessService]->[ゲートウェイの追加 CLASSPATH]をビルドされたDLL(絶対パスでbin/Release/PEX.dllを指定)に変更。  
[Remote BusinessService]->[リモート設定]のSERVERS=kafka:29092をSERVERS=irishost:9092に変更。  
> irishostはIRISコンテナが稼働しているホスト名

![4](https://raw.githubusercontent.com/IRISMeister/doc-images/main/pex-demo/win-bs-setting.png)  


以後、「Kafkaへの送信」の手順を実行します。

