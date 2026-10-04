package golden;

import com.alibaba.fastjson.JSONObject;
import com.xkcoding.http.config.HttpConfig;
import com.xkcoding.http.support.AbstractHttp;
import com.xkcoding.http.support.HttpHeader;
import com.xkcoding.http.support.SimpleHttpResponse;

import java.util.*;

/**
 * 记录 JustAuth 发出的请求，并按顺序回放 fixture 中的响应
 */
public class ReplayHttp extends AbstractHttp {
    private final Deque<String> responses;
    private final List<JSONObject> requests = new ArrayList<>();

    public ReplayHttp(List<String> responses) {
        super(new HttpConfig());
        this.responses = new ArrayDeque<>(responses);
    }

    public List<JSONObject> getRequests() {
        return requests;
    }

    private SimpleHttpResponse record(String method, String url, Map<String, String> params, String data, HttpHeader header, Boolean encode) {
        JSONObject request = new JSONObject(true);
        request.put("method", method);
        request.put("url", url);
        request.put("params", params == null ? null : new LinkedHashMap<>(params));
        request.put("data", data);
        request.put("encode", encode);
        request.put("headers", header == null ? new LinkedHashMap<>() : new LinkedHashMap<>(header.getHeaders()));
        requests.add(request);
        if (responses.isEmpty()) {
            throw new IllegalStateException("No replay response for request #" + requests.size() + ": " + method + " " + url);
        }
        return new SimpleHttpResponse(true, 200, new HashMap<>(), responses.poll(), null);
    }

    @Override
    public SimpleHttpResponse get(String url) {
        return record("GET", url, null, null, null, null);
    }

    @Override
    public SimpleHttpResponse get(String url, Map<String, String> params, boolean encode) {
        return record("GET", url, params, null, null, encode);
    }

    @Override
    public SimpleHttpResponse get(String url, Map<String, String> params, HttpHeader header, boolean encode) {
        return record("GET", url, params, null, header, encode);
    }

    @Override
    public SimpleHttpResponse post(String url) {
        return record("POST", url, null, null, null, null);
    }

    @Override
    public SimpleHttpResponse post(String url, String data) {
        return record("POST", url, null, data, null, null);
    }

    @Override
    public SimpleHttpResponse post(String url, String data, HttpHeader header) {
        return record("POST", url, null, data, header, null);
    }

    @Override
    public SimpleHttpResponse post(String url, Map<String, String> params, boolean encode) {
        return record("POST", url, params, null, null, encode);
    }

    @Override
    public SimpleHttpResponse post(String url, Map<String, String> params, HttpHeader header, boolean encode) {
        return record("POST", url, params, null, header, encode);
    }
}
