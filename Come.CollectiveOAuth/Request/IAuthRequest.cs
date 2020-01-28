using Come.CollectiveOAuth.Models;

namespace Come.CollectiveOAuth.Request
{
    /**
     * JustAuth {@code Request}公共接口，所有平台的{@code Request}都需要实现该接口
     * <p>
     * {@link AuthRequest#authorize()}
     * {@link AuthRequest#authorize(string)}
     * {@link AuthRequest#login(AuthCallback)}
     * {@link AuthRequest#revoke(AuthToken)}
     * {@link AuthRequest#refresh(AuthToken)}
     *
     * @author yadong.zhang (yadong.zhang0415(a)gmail.com)
     * @since 1.8
     */
    public interface IAuthRequest
    {
        /**
     * 返回授权url，可自行跳转页面
     * <p>
     * 不建议使用该方式获取授权地址，不带{@code state}的授权地址，容易受到csrf攻击。
     * 建议使用{@link AuthDefaultRequest#authorize(string)}方法生成授权地址，在回调方法中对{@code state}进行校验
     *
     * @return 返回授权地址
     */
        string authorize();

        /**
         * 返回带{@code state}参数的授权url，授权回调时会带上这个{@code state}
         *
         * @param state state 验证授权流程的参数，可以防止csrf
         * @return 返回授权地址
         */
        string authorize(string state);

        /**
         * 第三方登录
         *
         * @param authCallback 用于接收回调参数的实体
         * @return 返回登录成功后的用户信息
         */
        AuthResponse login(AuthCallback authCallback);

        /**
         * 撤销授权
         *
         * @param authToken 登录成功后返回的Token信息
         * @return AuthResponse
         */
        AuthResponse revoke(AuthToken authToken);

        /**
         * 刷新access token （续期）
         *
         * @param authToken 登录成功后返回的Token信息
         * @return AuthResponse
         */
        AuthResponse refresh(AuthToken authToken);
    }
}