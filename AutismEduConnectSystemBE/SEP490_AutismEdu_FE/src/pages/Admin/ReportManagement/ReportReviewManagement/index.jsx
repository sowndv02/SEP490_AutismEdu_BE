import VisibilityIcon from '@mui/icons-material/Visibility';
import { Box, FormControl, IconButton, InputLabel, MenuItem, Paper, Select, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { format } from 'date-fns';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import LoadingComponent from '~/components/LoadingComponent';
import TablePagging from '~/components/TablePagging';
import services from '~/plugins/services';
function ReportReviewManagement() {
    const [reports, setReports] = useState([]);
    const [loading, setLoading] = useState(false);
    const [status, setStatus] = useState("pending");
    const [pagination, setPagination] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const nav = useNavigate();
    useEffect(() => {
        handleGetReports()
    }, [])
    useEffect(() => {
        if (currentPage === 1) {
            handleGetReports();
        } else {
            setCurrentPage(1)
        }
    }, [status])
    useEffect(() => {
        handleGetReports();
    }, [currentPage])
    const handleGetReports = async () => {
        try {
            setLoading(true);
            await services.ReportManagementAPI.getListReportTutor((res) => {
                setReports(res.result);
                res.pagination.currentSize = res.result.length
                setPagination(res.pagination);
            }, (err) => {
                console.log(err);
            }, {
                pageNumber: currentPage,
                status: status,
                type: "review"
            })
            setLoading(false);
        } catch (error) {
            enqueueSnackbar("Lỗi hệ thống", { variant: "error" });
            setLoading(false);
        }
    }
    const getStatus = (status) => {
        let statusString = "";
        switch (status) {
            case 0:
                statusString = "Từ chối";
                break;
            case 1:
                statusString = "Chấp nhận";
                break;
            case 2:
                statusString = "Đang chờ";
                break;
        }
        return statusString;
    }
    return (
        <Paper variant='elevation' sx={{ p: 3 }}>
            <Typography variant='h4'>Quản lý đơn tố cáo đánh giá</Typography>
            <Box sx={{ display: "flex", gap: 5 }} mt={5}>
                <FormControl sx={{ width: '150px' }}>
                    <InputLabel id="status">Trạng thái đơn</InputLabel>
                    <Select value={status} onChange={(e) => setStatus(e.target.value)}
                        label="Trạng thái đơn"
                        labelId="status"
                    >
                        <MenuItem value="pending">Đang chờ</MenuItem>
                        <MenuItem value="approve">Đã chấp nhận</MenuItem>
                        <MenuItem value="reject">Đã từ chối</MenuItem>
                    </Select>
                </FormControl>
            </Box>
            <TableContainer component={Paper} sx={{ mt: 5 }}>
                <Table sx={{ minWidth: 650 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell>STT</TableCell>
                            <TableCell>Người tố cáo</TableCell>
                            <TableCell>Người bị tố cáo</TableCell>
                            <TableCell sx={{ maxWidth: "200px" }} align="left">Lý do tố cáo</TableCell>
                            <TableCell align="left">Trạng thái</TableCell>
                            <TableCell align="center">Ngày tạo</TableCell>
                            <TableCell align="center"></TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {
                            reports.length !== 0 && reports.map((b, index) => {
                                return (
                                    <TableRow key={b.id}>
                                        <TableCell>
                                            {index + 1 + (currentPage - 1) * 10}
                                        </TableCell>
                                        <TableCell>
                                            {b.reporter.email}
                                        </TableCell>
                                        <TableCell>
                                            {b.review.parent.email}
                                        </TableCell>
                                        <TableCell align="left" sx={{ maxWidth: "200px" }}>
                                            {b.description}
                                        </TableCell>
                                        <TableCell sx={{ color: b.status === 1 ? "green" : b.status === 2 ? "blue" : "red" }}>
                                            {getStatus(b.status)}
                                        </TableCell>
                                        <TableCell align="center">
                                            {format(b?.createdDate || "01/01/2024", "dd/MM/yyyy")}
                                        </TableCell>
                                        <TableCell align="center">
                                            <IconButton onClick={() => nav('/admin/report-review-management/' + b.id)}>
                                                <VisibilityIcon />
                                            </IconButton>
                                        </TableCell>
                                    </TableRow>
                                )
                            })
                        }
                    </TableBody>
                </Table>
                <TablePagging pagination={pagination} setPagination={setPagination} setCurrentPage={setCurrentPage} />
            </TableContainer>
            <LoadingComponent open={loading} />
        </Paper>
    )
}

export default ReportReviewManagement
