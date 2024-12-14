const checkValid = (value, field, setError, password) => {
    const rgPassword = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@$?_-]).+$/
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    if (field === 1) {
        if (value === "") {
            setError("Bắt buộc");
            return false;
        } else if (!emailRegex.test(value)) {
            setError("Email không hợp lệ");
            return false;
        } else if (value.length > 320) {
            setError("Email quá dài");
            return false;
        } else {
            setError(null);
            return true;
        }
    }
    if (field === 2) {
        if (value === "") {
            setError("Bắt buộc");
            return false;
        } else if (value.length < 8) {
            setError("Mật khẩu phải dài hơn 8 ký tự");
            return false;
        } else if (value.length > 15) {
            setError("Mật khẩu phải nhỏ hơn 15 ký tự");
            return false;
        } else if (!rgPassword.test(value)) {
            setError("Mật khẩu không hợp lệ!");
            return false;
        } else {
            setError(null);
            return true;
        }
    }
    if (field === 3) {
        if (value === "") {
            setError("Bắt buộc");
            return false;
        } else if (value !== password) {
            setError("Không trùng với mật khẩu");
            return false;
        } else {
            setError(null);
            return true;
        }
    }
}

export default checkValid;