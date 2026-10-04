# JustAuth 标准答案生成器

读取 `Come.CollectiveOAuth.Tests/Fixtures/*.json`，用 JustAuth 1.16.7 执行授权/登录/刷新/撤销，
把授权地址、发出的请求、返回结果写入 `Come.CollectiveOAuth.Tests/Golden/`。

    cd tools/justauth-golden
    mvn -q compile exec:java -Dexec.args="../../Come.CollectiveOAuth.Tests/Fixtures ../../Come.CollectiveOAuth.Tests/Golden"

需要 JDK 8+ 与 Maven。新增或修改 fixture 后重新运行，并提交生成的 golden 文件。
