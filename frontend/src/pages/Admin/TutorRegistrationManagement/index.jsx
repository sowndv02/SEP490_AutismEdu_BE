import { Box, FormControl, InputLabel, MenuItem, Select, TextField, Typography } from '@mui/material';
import TutorRegistrationTable from './TutorRegistrationTable';
function TutorRegistrationManagement() {
    return (
        <Box sx={{
            height: (theme) => `calc(100vh - ${theme.myapp.adminHeaderHeight})`,
            width: "100%",
            position: "relative",
            marginTop: (theme) => theme.myapp.adminHeaderHeight
        }}>
            <Box sx={{
                width: "100%", bgcolor: "white", p: "20px",
                borderRadius: "10px",
                boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px"
            }}>
                <Typography variant='h6'>Danh sách đơn đăng ký</Typography>
                <Box sx={{
                    width: "100%",
                    display: "flex",
                    justifyContent: "space-between",
                    gap: 2
                }}
                    mt={3}
                >
                    <FormControl sx={{
                        width: "200px"
                    }}>
                        <InputLabel id="statuslabel">Trạng thái</InputLabel>
                        <Select
                            labelId="statuslabel"
                            id="status"
                            label="Trạng thái"
                            size='small'
                        >
                            <MenuItem value={10}>Đang chờ</MenuItem>
                            <MenuItem value={20}>Đã phê duyệt</MenuItem>
                            <MenuItem value={30}>Đã từ chối</MenuItem>
                        </Select>
                    </FormControl>
                    <TextField id="outlined-basic" label="Tìm người dùng" variant="outlined"
                        sx={{
                            padding: "0",
                            width: "300px"
                        }} size='small' />
                </Box>
                <Box>
                    <TutorRegistrationTable />
                </Box>
            </Box>
        </Box>
    )
}

export default TutorRegistrationManagement
