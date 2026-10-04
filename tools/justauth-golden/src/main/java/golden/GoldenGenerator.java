package golden;

import com.alibaba.fastjson.JSON;
import com.alibaba.fastjson.JSONArray;
import com.alibaba.fastjson.JSONObject;
import com.alibaba.fastjson.serializer.SerializerFeature;
import com.xkcoding.http.HttpUtil;
import me.zhyd.oauth.cache.AuthStateCache;
import me.zhyd.oauth.config.AuthConfig;
import me.zhyd.oauth.config.AuthDefaultSource;
import me.zhyd.oauth.exception.AuthException;
import me.zhyd.oauth.model.AuthCallback;
import me.zhyd.oauth.model.AuthResponse;
import me.zhyd.oauth.model.AuthToken;
import me.zhyd.oauth.request.AuthRequest;

import java.io.File;
import java.lang.reflect.InvocationTargetException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.*;

/**
 * 用法：mvn -q compile exec:java -Dexec.args="<fixturesDir> <goldenDir>"
 */
public class GoldenGenerator {

    /** 测试用内存 state 缓存，保证每个场景互不影响 */
    static class MemoryStateCache implements AuthStateCache {
        private final Map<String, String> map = new HashMap<>();
        public void cache(String key, String value) { map.put(key, value); }
        public void cache(String key, String value, long timeout) { map.put(key, value); }
        public String get(String key) { return map.get(key); }
        public boolean containsKey(String key) { return map.containsKey(key); }
    }

    public static void main(String[] args) throws Exception {
        File fixturesDir = new File(args[0]);
        File goldenDir = new File(args[1]);
        goldenDir.mkdirs();
        File[] files = fixturesDir.listFiles((d, n) -> n.endsWith(".json"));
        Arrays.sort(files);
        for (File file : files) {
            JSONObject fixture = JSON.parseObject(new String(Files.readAllBytes(file.toPath()), StandardCharsets.UTF_8));
            JSONObject golden = run(fixture);
            String text = JSON.toJSONString(golden, SerializerFeature.PrettyFormat, SerializerFeature.DisableCircularReferenceDetect);
            Files.write(new File(goldenDir, file.getName()).toPath(), text.getBytes(StandardCharsets.UTF_8));
            System.out.println("golden: " + file.getName());
        }
    }

    private static AuthRequest newRequest(AuthDefaultSource source, AuthConfig config, AuthStateCache cache) throws Exception {
        try {
            return source.getTargetClass().getConstructor(AuthConfig.class, AuthStateCache.class).newInstance(config, cache);
        } catch (InvocationTargetException e) {
            throw (Exception) e.getCause();
        }
    }

    private static JSONObject error(Exception e) {
        JSONObject err = new JSONObject(true);
        err.put("type", e.getClass().getName());
        if (e instanceof AuthException) {
            err.put("code", ((AuthException) e).getErrorCode());
            err.put("msg", ((AuthException) e).getErrorMsg());
        } else {
            err.put("msg", e.getMessage());
        }
        return err;
    }

    static JSONObject run(JSONObject fixture) throws Exception {
        AuthDefaultSource source = AuthDefaultSource.valueOf(fixture.getString("source"));
        AuthConfig config = fixture.getJSONObject("config").toJavaObject(AuthConfig.class);
        String state = fixture.getString("state");

        JSONObject golden = new JSONObject(true);
        golden.put("source", source.name());

        // 部分平台（如 Twitter）生成授权地址时也会发请求，回放 fixture 中的 authorizeResponses
        List<String> authorizeResponses = fixture.containsKey("authorizeResponses")
            ? fixture.getJSONArray("authorizeResponses").toJavaList(String.class)
            : Collections.<String>emptyList();

        // 授权地址
        ReplayHttp authorizeHttp = new ReplayHttp(authorizeResponses);
        HttpUtil.setHttp(authorizeHttp);
        try {
            AuthRequest request = newRequest(source, config, new MemoryStateCache());
            golden.put("constructorError", null);
            golden.put("authorizeUrl", request.authorize(state));
            if (!authorizeHttp.getRequests().isEmpty()) {
                golden.put("authorizeRequests", authorizeHttp.getRequests());
            }
        } catch (Exception e) {
            golden.put("constructorError", error(e));
            golden.put("authorizeUrl", null);
            golden.put("scenarios", new JSONArray());
            return golden;
        }

        JSONArray scenarios = new JSONArray();
        for (Object item : fixture.getJSONArray("scenarios")) {
            JSONObject scenario = (JSONObject) item;
            List<String> responses = scenario.getJSONArray("responses").toJavaList(String.class);
            ReplayHttp http = new ReplayHttp(responses);

            JSONObject out = new JSONObject(true);
            out.put("name", scenario.getString("name"));
            try {
                MemoryStateCache cache = new MemoryStateCache();
                AuthRequest request = newRequest(source, config, cache);
                HttpUtil.setHttp(new ReplayHttp(authorizeResponses));
                request.authorize(state);
                HttpUtil.setHttp(http);
                AuthResponse<?> result;
                switch (scenario.getString("action")) {
                    case "login":
                        result = request.login(scenario.getJSONObject("callback").toJavaObject(AuthCallback.class));
                        break;
                    case "refresh":
                        result = request.refresh(scenario.getJSONObject("token").toJavaObject(AuthToken.class));
                        break;
                    case "revoke":
                        result = request.revoke(scenario.getJSONObject("token").toJavaObject(AuthToken.class));
                        break;
                    default:
                        throw new IllegalArgumentException("unknown action " + scenario.getString("action"));
                }
                out.put("result", JSON.toJSON(result));
                out.put("error", null);
            } catch (Exception e) {
                out.put("result", null);
                out.put("error", error(e));
            }
            out.put("requests", http.getRequests());
            scenarios.add(out);
        }
        golden.put("scenarios", scenarios);
        return golden;
    }
}
