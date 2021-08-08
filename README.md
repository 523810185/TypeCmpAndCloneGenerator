版本更新日志
-------------------------------------------------------------------------
2021.8.6
最初的版本。目前存在的问题：
1. 有field为class的时候，没有判空。（当然，作为Unity序列化数据的使用是没有问题的）
2. 当存在一个field，继承自List<T>的时候，不会被认为是数组类型的，后面应当使用类似于Odin中TypeExtension中判断一个类型是否继承自List<T>的做法来实现数组类型的相关操作。
3. 有些ILEmit中代码太长了，应当抽取常用部分，构建面向对象的IL编程。
4. 关于Clone方法，后面需要想办法使得第一个参数带ref，使得更方便的使用，否则，参数中带有null的一些情况可能不能令人满意。但是测试一下发现lambda中参数似乎不能用ref修饰。后面可能需要考虑类似于在Clone方法的参数中带一个上带一个父类上下文，来避免这个问题。
-------------------------------------------------------------------------
2021.8.8
更新如下：
  修复的问题：
  1. 检查了field为class的情况下，有null存在的情况。
  2. 对il提供了一些便利的扩展（例如if-then-else块等等），后续需要扩展更多常用的语句，并重构以前的代码结构。
  发现的新问题：
  1. 在处理class为null的情况突然发现，解析数组类型的时候，只考虑了作为field存在的时候，没有考虑来源也为数组的时候，例如List<List<T>>。（总结一下，只要当前域可能为null，就要处理三种情况的来源，作为顶层参数，作为常规field，作为数组的item）
  2. 发现没有处理field为高维数组的情况。
