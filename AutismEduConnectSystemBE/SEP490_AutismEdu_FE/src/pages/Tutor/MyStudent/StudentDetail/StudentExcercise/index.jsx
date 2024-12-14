import MinimizeIcon from '@mui/icons-material/Minimize';
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import {
    Box, Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Divider, FormControl, IconButton, MenuItem, Pagination, Select, Stack,
    TextField,
    Typography
} from '@mui/material';
import Paper from '@mui/material/Paper';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import { format } from 'date-fns';
import { useEffect, useState } from 'react';
import emptyBook from '~/assets/images/icon/emptybook.gif';
import services from '~/plugins/services';
function StudentExcercise({ studentProfile }) {
    const [schedules, setSchedules] = useState([]);
    const [openDialogE, setOpenDialogE] = useState(false);
    const [selectedExcercise, setSelectedExcercise] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const [total, setTotal] = useState(1);
    const [passingStatus, setPassingStatus] = useState("ALL");
    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState("");
    const [loading, setLoading] = useState(false);
    useEffect(() => {
        handleGetSchedules();
    }, [currentPage]);

    useEffect(() => {
        if (currentPage === 1) {
            handleGetSchedules();
        } else setCurrentPage(1);
    }, [passingStatus, endDate, startDate])
    const handleGetSchedules = async () => {
        try {
            setLoading(true);
            await services.ScheduleAPI.getAssignSchedule((res) => {
                setTotal(Math.floor(res.pagination.total / 10) + 1);
                setSchedules(res.result);
            }, (err) => {
                console.log(err);
            }, {
                pageNumber: currentPage,
                studentProfileId: studentProfile.id,
                status: passingStatus,
                startDate: startDate,
                endDate: endDate
            })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    }

    const getStatus = (index) => {
        const now = Date.now();
        const scheduleDate = new Date(schedules[index].scheduleDate);
        if (scheduleDate > now) {
            return 3;
        } else {
            return schedules[index].passingStatus;
        }
    }
    const handleChange = (event, value) => {
        setCurrentPage(value);
    };

    return (
        <Box sx={{ px: 5, py: 3 }}>
            <Typography variant='h4'>Lịch sử học tập của trẻ</Typography>
            <Box sx={{
                width: "100%",
                display: "flex",
                justifyContent: "space-between",
                gap: 2,
                alignItems: "center"
            }}
                mt={3}
            >
                <FormControl sx={{ mt: 3, width: "200px" }}>
                    <Select value={passingStatus} onChange={(e) => setPassingStatus(e.target.value)}>
                        <MenuItem value="ALL">Tất cả</MenuItem>
                        <MenuItem value="NOT_YET">Chưa có đánh giá</MenuItem>
                        <MenuItem value="PASSED">Đã đạt</MenuItem>
                        <MenuItem value="NOT_PASSED">Chưa đạt</MenuItem>
                    </Select>
                </FormControl>
                <Stack direction='row' gap={3}>
                    <Stack direction="row" gap={1} alignItems="center">
                        <Typography>Từ ngày</Typography>
                        <TextField
                            value={startDate}
                            onChange={(e) => setStartDate(e.target.value)}
                            sx={{
                                padding: "0"
                            }} size='small'
                            type='date'
                        />
                    </Stack>
                    <MinimizeIcon />
                    <Stack direction="row" gap={1} alignItems="center">
                        <Typography>Đến ngày</Typography>
                        <TextField
                            value={endDate}
                            onChange={(e) => setEndDate(e.target.value)}
                            sx={{
                                padding: "0"
                            }} size='small'
                            type='date'
                        />
                    </Stack>
                </Stack>
            </Box>
            {
                (!schedules || schedules.length === 0) ? (
                    <Stack width="100%" alignItems="center" justifyContent="center" mt="100px">
                        <Box sx={{ textAlign: "center" }}>
                            <img src={emptyBook} style={{ height: "200px" }} />
                            <Typography>Không có bài tập nào!</Typography>
                        </Box>
                    </Stack>
                ) : (
                    <>
                        <TableContainer component={Paper} sx={{ mt: 2 }}>
                            <Table sx={{ minWidth: 650 }} aria-label="simple table">
                                <TableHead>
                                    <TableRow>
                                        <TableCell sx={{ fontWeight: "bold" }}>STT</TableCell>
                                        <TableCell sx={{ maxWidth: "200px", fontWeight: "bold" }}>Tên bài tập</TableCell>
                                        <TableCell sx={{ maxWidth: "200px", fontWeight: "bold" }}>Loại bài tập</TableCell>
                                        <TableCell sx={{ fontWeight: "bold" }}>Ngày học</TableCell>
                                        <TableCell sx={{ fontWeight: "bold" }}>Trạng thái</TableCell>
                                        <TableCell sx={{ fontWeight: "bold" }} align='center'>Xem chi tiết</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {
                                        schedules && schedules.length !== 0 && schedules.map((row, index) => (
                                            <TableRow
                                                key={row.id}
                                                sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                                            >
                                                <TableCell component="th" scope="row">
                                                    {index + (currentPage - 1) * 10 + 1}
                                                </TableCell>
                                                <TableCell sx={{ maxWidth: "200px" }}>{row.exercise.exerciseName}</TableCell>
                                                <TableCell sx={{ maxWidth: "200px" }}>{row.exerciseType.exerciseTypeName}</TableCell>
                                                <TableCell>{format(new Date(row.scheduleDate), "dd/MM/yyyy")}</TableCell>
                                                <TableCell>
                                                    <span
                                                        style={{
                                                            backgroundColor: getStatus(index) === 3 ? '#d1ecf1' : getStatus(index) === 2 ? '#fff3cd' : getStatus(index) === 1 ? '#d4edda' : '#f8d7da',
                                                            color: getStatus(index) === 3 ? 'blue' : getStatus(index) === 2 ? "orange" : getStatus(index) === 1 ? "green" : "red",
                                                            padding: '4px 8px',
                                                            borderRadius: '8px',
                                                            fontWeight: 'bold',
                                                            display: 'inline-block'
                                                        }}
                                                    >
                                                        {getStatus(index) === 3 ? "Chưa học" : getStatus(index) === 2 ? "Không có đánh giá" : getStatus(index) === 1 ? "Đạt" : "Không đạt"}
                                                    </span>
                                                </TableCell>
                                                <TableCell align='center'>
                                                    <IconButton onClick={() => { setSelectedExcercise(row); setOpenDialogE(true) }}>
                                                        <RemoveRedEyeIcon />
                                                    </IconButton>
                                                </TableCell>
                                            </TableRow>
                                        ))
                                    }
                                </TableBody>
                            </Table>
                        </TableContainer>
                        <Stack direction='row' justifyContent="center">
                            <Pagination count={total} color="secondary" sx={{ mt: 5 }} page={currentPage} onChange={handleChange} />
                        </Stack>
                    </>
                )
            }
            {selectedExcercise && <Dialog
                open={openDialogE}
                onClose={() => setOpenDialogE(false)}
                maxWidth="md"
                fullWidth
                sx={{
                    '& .MuiDialog-paper': {
                        borderRadius: '12px',
                        boxShadow: '0 4px 20px rgba(0, 0, 0, 0.2)',
                        padding: '16px',
                    }
                }}
            >
                <DialogTitle
                    textAlign="center"
                    sx={{
                        fontSize: '1.5rem',
                        fontWeight: 'bold',
                        color: '#3c4ff4',
                    }}
                >
                    {selectedExcercise.exercise.exerciseName}
                </DialogTitle>
                <Divider sx={{ margin: '16px 0' }} />
                <DialogContent>
                    <Stack
                        direction="row"
                        gap={2}
                        sx={{
                            marginBottom: '16px',
                            alignItems: 'center',
                        }}
                    >
                        <Typography
                            sx={{
                                width: '25%',
                                fontWeight: 'bold',
                                color: '#b660ec',
                            }}
                        >
                            Ghi chú từ giảng viên:
                        </Typography>
                        {!selectedExcercise.note ? (
                            <Typography
                                sx={{
                                    color: 'orange',
                                    fontStyle: 'italic',
                                }}
                            >
                                Không có ghi chú
                            </Typography>
                        ) : (
                            <Typography
                                sx={{
                                    color: 'orange',
                                    width: '70%',
                                    fontStyle: 'italic',
                                    wordBreak: 'break-word',
                                }}
                            >
                                {selectedExcercise.note}
                            </Typography>
                        )}
                    </Stack>
                    <Box
                        mx="auto"
                        width="90%"
                        sx={{
                            padding: '8px',
                            backgroundColor: '#f9f9f9',
                            borderRadius: '8px',
                            boxShadow: 'inset 0 0 10px rgba(0, 0, 0, 0.1)',
                            overflow: 'hidden',
                        }}
                        dangerouslySetInnerHTML={{ __html: selectedExcercise.exercise.description }}
                    />
                </DialogContent>
                <Divider sx={{ margin: '16px 0' }} />
                <DialogActions>
                    <Button
                        onClick={() => setOpenDialogE(false)}
                        variant="contained"
                        color="primary"
                        sx={{
                            padding: '8px 16px',
                            borderRadius: '8px',
                        }}
                    >
                        Đóng
                    </Button>
                </DialogActions>
            </Dialog>
            }
        </Box>
    )
}

export default StudentExcercise
