# sins

minecraftのbungeecordを　子プロセスとして起動→なかよし　するコード  
このコードでは6011番のポートをplugin用、6001番のポートをdiscord bot用のポートにしているよ(今のところ違いがないんだけどね)

データ受信時の動作は簡単でコンテナ起動・停止用のシェルにコンテナ名を渡して仕事してもらうか、受け取ったデータを直接bungeecordに打ち込んでるだけ  
多分C#で遊び始めて1時間もあれば分かるくらい単純で、雑にもほどがあるコードです。

導入方法(ubuntuの場合)  
sinsのコード内で変更する必要のある部分を書き換える(ファイルパスなど)  
monoでsinsのコードをコンパイルし、出力されたexeファイルをbungeecordと同じフォルダ内もしくは専用のフォルダに入れておく  
sinsを入れたフォルダ内にdockerコンテナの起動・停止用のシェルを作成する。  
  
  
作成するシェルの例(起動)：  
  
#!/bin/bash  
/usr/bin/docker start $1
  
作成するシェルの例(停止)：  
  
#!/bin/bash  　
/usr/bin/docker stop $1  
  
  
monoでsinsを実行し、任意でminecraftサーバーとdiscord botを起動する。(この時、minecraftサーバーはstplug(最下段に記述)を導入済み　discord botはsins用に通信ができるものとする)
  
以上でsinsの導入は完了です。
  
stplug https://github.com/D-R-D/stplug
