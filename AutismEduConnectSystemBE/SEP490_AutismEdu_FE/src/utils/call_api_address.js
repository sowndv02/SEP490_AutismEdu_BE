import axios from "axios";

const CALL_API_ADDRESS = async(name) => {
    try {
        const res = await axios.get(`https://vietnam-administrative-division-json-server-swart.vercel.app/${name}`);
        return res.data;
    } catch (error) {
        console.log(error.message);    
    }
}
export default CALL_API_ADDRESS;