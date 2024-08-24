const checkValid = (value, field, setError, password) => {
    const rgPassword = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[.!&%]).+$/
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    if (field === 1) {
        if (value === "") {
            setError("Please enter email");
            return false;
        } else if (!emailRegex.test(value)) {
            setError("Email is not valid");
            return false;
        } else {
            setError(null);
            return true;
        }
    }
    if (field === 2) {
        if (value === "") {
            setError("Please enter password");
            return false;
        } else if (value.length < 8) {
            setError("Password must be more than 8 characters");
            return false;
        } else if (value.length > 15) {
            setError("Password must be less than 15 characters");
            return false;
        } else if (!rgPassword.test(value)) {
            setError("Password is invalid!");
            return false;
        } else {
            setError(null);
            return true;
        }
    }
    if (field === 3) {
        if (value === "") {
            setError("Please enter confirm password");
            return false;
        } else if (value !== password) {
            setError("Confirm password doesn't match with the password");
            return false;
        } else {
            setError(null);
            return true;
        }
    }
}

export default checkValid;