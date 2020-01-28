using System;

/**
 * OAuth平台的API地址的统一接口，提供以下方法：
 * 1) {@link AuthSource#authorize()}: 获取授权url. 必须实现
 * 2) {@link AuthSource#accessToken()}: 获取accessToken的url. 必须实现
 * 3) {@link AuthSource#userInfo()}: 获取用户信息的url. 必须实现
 * 4) {@link AuthSource#revoke()}: 获取取消授权的url. 非必须实现接口（部分平台不支持）
 * 5) {@link AuthSource#refresh()}: 获取刷新授权的url. 非必须实现接口（部分平台不支持）
 * <p>
 * 注：
 * ①、如需通过JustAuth扩展实现第三方授权，请参考{@link AuthDefaultSource}自行创建对应的枚举类并实现{@link AuthSource}接口
 * ②、如果不是使用的枚举类，那么在授权成功后获取用户信息时，需要单独处理source字段的赋值
 * ③、如果扩展了对应枚举类时，在{@link me.zhyd.oauth.request.AuthRequest#login(AuthCallback)}中可以通过{@code xx.toString()}获取对应的source
 *
 * @author wei.fu (wei.fu@rthinkingsoft.cn)
 * @version 1.0
 * @since 1.12.0
 */

namespace Come.CollectiveOAuth.Config
{
    public interface IAuthSource
    {
        /**
         * 授权的api
         *
         * @return url
         */
        string authorize();

        /**
         * 获取accessToken的api
         *
         * @return url
         */
        string accessToken();

        /**
         * 获取用户信息的api
         *
         * @return url
         */
        string userInfo();

        /**
         * 取消授权的api
         *
         * @return url
         */
        string revoke();


        /**
         * 刷新授权的api
         *
         * @return url
         */
        string refresh();

        /**
         * 获取Source的字符串名字
         *
         * @return name
         */
        string getName();
    }
}