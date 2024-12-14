import { Box, Button, FormControl, InputLabel, MenuItem, Select, Stack, TextField, Typography } from '@mui/material';
import TutorRegistrationTable from './TutorRegistrationTable';
import { useEffect, useState } from 'react';
import MinimizeIcon from '@mui/icons-material/Minimize';
import SearchIcon from '@mui/icons-material/Search';
function TutorRegistrationManagement() {
    const [status, setStatus] = useState(10);
    const [searchValue, setSearchValue] = useState("");
    const [submit, setSubmit] = useState(true);
    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState(new Date().toISOString().split('T')[0]);
    return (
        <Box>
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
                    gap: 2,
                    alignItems: "center"
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
                            value={status}
                            onChange={(e) => setStatus(e.target.value)}
                        >
                            <MenuItem value={10}>Đang chờ</MenuItem>
                            <MenuItem value={20}>Đã phê duyệt</MenuItem>
                            <MenuItem value={30}>Đã từ chối</MenuItem>
                        </Select>
                    </FormControl>
                    <Stack direction='row' gap={3}>
                        <Stack direction="row" gap={1} justifyItems="center">
                            <Typography>Từ ngày</Typography>
                            <TextField
                                value={startDate}
                                onChange={(e) => setStartDate(e.target.value)}
                                sx={{
                                    padding: "0"
                                }} size='small'
                                type='date'
                                inputProps={{ max: new Date().toISOString().split('T')[0] }}
                            />
                        </Stack>
                        <MinimizeIcon />
                        <Stack direction="row" gap={1} justifyItems="center">
                            <Typography>Đến ngày</Typography>
                            <TextField
                                value={endDate}
                                onChange={(e) => setEndDate(e.target.value)}
                                sx={{
                                    padding: "0"
                                }} size='small'
                                type='date'
                                inputProps={{ max: new Date().toISOString().split('T')[0] }}
                            />
                        </Stack>
                    </Stack>
                    <TextField id="outlined-basic" label="Tìm người dùng" variant="outlined"
                        sx={{
                            padding: "0",
                            width: "300px"
                        }} size='small'
                        value={searchValue}
                        onChange={(e) => setSearchValue(e.target.value)}
                    />
                </Box>
                <Button variant='contained' sx={{ mt: 3 }} startIcon={<SearchIcon />}
                    onClick={() => setSubmit(!submit)}
                >Tìm kiếm
                </Button>
                <Box>
                    <TutorRegistrationTable status={status} searchValue={searchValue} submit={submit}
                        startDate={startDate}
                        endDate={endDate}
                    />
                </Box>
            </Box>
        </Box>
    )
}

export default TutorRegistrationManagement
