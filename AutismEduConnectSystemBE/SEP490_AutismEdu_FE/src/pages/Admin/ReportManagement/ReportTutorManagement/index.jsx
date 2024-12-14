import { Box, FormControl, InputLabel, MenuItem, Paper, Select, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import LoadingComponent from '~/components/LoadingComponent';
import TablePagging from '~/components/TablePagging';
import services from '~/plugins/services';
import PAGES from '~/utils/pages';
function ReportTutorManagement() {
    const [reports, setReports] = useState([]);
    const [loading, setLoading] = useState(false);
    const [status, setStatus] = useState("all");
    const [searchName, setSearchName] = useState("");
    const [pagination, setPagination] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const nav = useNavigate();
    useEffect(() => {
        handleGetReports();
    }, [])
    useEffect(() => {
        handleGetReports();
    }, [currentPage])
    useEffect(() => {
        if (currentPage === 1) {
            handleGetReports();
        } else {
            setCurrentPage(1)
        }
    }, [status])

    useEffect(() => {
        const handler = setTimeout(() => {
            if (currentPage === 1) {
                handleGetReports();
            } else {
                setCurrentPage(1)
            }
        }, 1000)
        return () => {
            clearTimeout(handler)
        }
    }, [searchName])
    const formatDate = (date) => {
        if (!date) return "";
        const d = new Date(date);
        return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }
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
                type: "tutor",
                reportTutorType: 0,
                search: searchName
            })
            setLoading(false);
        } catch (error) {
            enqueueSnackbar("Lỗi hệ thống", { variant: "error" });
            setLoading(false);
        }
    }

    const getReportReason = (type) => {
        let reportReason = "";
        switch (type) {
            case 1:
                reportReason = "Không đáp ứng đúng yêu cầu về chuyên môn";
                break;
            case 2:
                reportReason = "Không có sự kiên nhẫn hoặc thái độ không phù hợp";
                break;
            case 3:
                reportReason = "Không đảm bảo lịch học đúng giờ";
                break;
            case 4:
                reportReason = "Thiếu giao tiếp với phụ huynh";
                break;
            case 5:
                reportReason = "Có dấu hiệu không trung thực hoặc vi phạm đạo đức nghề nghiệp";
                break;
            case 6:
                reportReason = "Lý do khác";
                break;
        }
        return reportReason;
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
            <Typography variant='h4'>Quản lý đơn tố cáo gia sư</Typography>
            <Box sx={{ display: "flex", gap: 5 }} mt={5}>
                <FormControl sx={{ width: '150px' }}>
                    <InputLabel id="status">Trạng thái đơn</InputLabel>
                    <Select value={status} onChange={(e) => setStatus(e.target.value)}
                        label="Trạng thái đơn"
                        labelId="status"
                    >
                        <MenuItem value="all">Tất cả</MenuItem>
                        <MenuItem value="pending">Đang chờ</MenuItem>
                        <MenuItem value="approve">Đã chấp nhận</MenuItem>
                        <MenuItem value="reject">Đã từ chối</MenuItem>
                    </Select>
                </FormControl>
                <TextField
                    sx={{ width: "350px" }}
                    label="Tìm kiếm theo tên hoặc email gia sư"
                    value={searchName}
                    onChange={(e) => setSearchName(e.target.value)}
                />
            </Box>
            <TableContainer component={Paper} sx={{ mt: 5 }}>
                <Table sx={{ minWidth: 650 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell>STT</TableCell>
                            <TableCell sx={{ maxWidth: "300px" }}>Tiêu đề</TableCell>
                            <TableCell>Người tố cáo</TableCell>
                            <TableCell>Người bị tố cáo</TableCell>
                            <TableCell sx={{ maxWidth: "200px" }} align="left">Lý do tố cáo</TableCell>
                            <TableCell align="left">Trạng thái</TableCell>
                            <TableCell align="center">Ngày tạo</TableCell>
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
                                        <TableCell sx={{ maxWidth: "200px" }}>
                                            <Link to={PAGES.REPORT_TUTOR_MANAGEMENT + "/detail/" + b.id} style={{ textDecoration: 'underline', color: "blue" }}>
                                                <Typography sx={{ wordBreak: "break-word" }}>
                                                    {b.title}
                                                </Typography>
                                            </Link>
                                        </TableCell>
                                        <TableCell>
                                            {b.reporter.email}
                                        </TableCell>
                                        <TableCell>
                                            {b.tutor.email}
                                        </TableCell>
                                        <TableCell align="left" sx={{ maxWidth: "200px" }}>
                                            {getReportReason(b.reportType)}
                                        </TableCell>
                                        <TableCell align="center" sx={{ color: b.status === 1 ? "green" : b.status === 2 ? "blue" : "red" }}>
                                            {getStatus(b.status)}
                                        </TableCell>
                                        <TableCell align="center">
                                            {formatDate(b.createdDate)}
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

export default ReportTutorManagement
